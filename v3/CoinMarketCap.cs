using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace GGo_v3
{
    public class CoinMarketCap
    {
        private readonly string _apiKey = "895db1f0-e095-49dd-8576-233118220705";
        private readonly string _urlQuotes = "https://pro-api.coinmarketcap.com/v2/cryptocurrency/quotes/latest";

        // 이벤트: (심볼, 시총 KRW) 리스트 반환
        // *주의: Name 자리에 '심볼'을 넣습니다. (예: BTC, DOGE)
        public event EventHandler<List<(string Name, decimal MarketCap)>> MarketCapReceived;

        /// <summary>
        /// 원하는 코인 심볼들만 시총(KRW) 가져오기.
        /// 예: RequestBySymbols(new[]{"BTC","ETH","XRP"})
        /// </summary>
        public async void RequestBySymbols(IEnumerable<string> symbols)
        {
            var data = await GetMarketCapsBySymbolsAsync(symbols);
            MarketCapReceived?.Invoke(this, data);
        }

        /// <summary>
        /// 특정 심볼들만 시총(KRW) 조회 (100개씩 청크 요청)
        /// 반환 튜플의 Name 자리에는 '심볼'이 들어갑니다.
        /// </summary>
        private async Task<List<(string Name, decimal MarketCap)>> GetMarketCapsBySymbolsAsync(
            IEnumerable<string> symbols, int chunkSize = 100)
        {
            var result = new List<(string, decimal)>();
            if (symbols == null) return result;

            var symList = symbols
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s.Trim().ToUpperInvariant())
                .Distinct()
                .ToList();

            for (int i = 0; i < symList.Count; i += chunkSize)
            {
                var chunk = symList.Skip(i).Take(chunkSize).ToList();
                string joined = string.Join(",", chunk);

                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("X-CMC_PRO_API_KEY", _apiKey);

                    string url = $"{_urlQuotes}?symbol={joined}&convert=KRW";
                    var resp = await client.GetAsync(url);
                    var json = await resp.Content.ReadAsStringAsync();

                    var obj = JObject.Parse(json);

                    // (선택) 상태 체크
                    var status = obj["status"];
                    if (status?["error_code"]?.Value<int>() != 0)
                    {
                        // 필요시 status["error_message"] 로깅
                        continue;
                    }

                    var data = obj["data"] as JObject;
                    if (data == null) continue;

                    // data: { "BTC": [ {...} ], "ETH": [ {...} ], ... }
                    foreach (var kv in data)
                    {
                        var arr = kv.Value as JArray;
                        if (arr == null || arr.Count == 0) continue;

                        var item = arr[0];

                        // 심볼 우선 반환 (키가 가장 정확)
                        string symbol = item["symbol"]?.ToString()?.ToUpperInvariant() ?? kv.Key;

                        decimal mc = ReadDecimalOrZero(item["quote"]?["KRW"]?["market_cap"]);

                        // Name 자리에 '심볼'을 넣어 이벤트로 전달
                        result.Add((symbol, mc));
                    }
                }

                await Task.Delay(300); // 레이트리밋 완화
            }

            return result;
        }

        /// <summary>
        /// JToken -> decimal 안전 변환 (null/문자열/정수/실수 모두 처리, 실패시 0)
        /// </summary>
        private static decimal ReadDecimalOrZero(JToken token)
        {
            if (token == null || token.Type == JTokenType.Null)
                return 0m;

            if (token.Type == JTokenType.Float || token.Type == JTokenType.Integer)
                return token.Value<decimal?>() ?? 0m;

            if (token.Type == JTokenType.String)
            {
                var s = token.Value<string>();
                if (decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var v))
                    return v;
                if (decimal.TryParse(s, NumberStyles.Any, CultureInfo.CurrentCulture, out v))
                    return v;
            }

            return 0m;
        }
    }
}
