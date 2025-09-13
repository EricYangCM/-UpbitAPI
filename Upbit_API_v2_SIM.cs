using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Drawing;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Policy;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web.UI.WebControls;
using System.Windows.Forms.DataVisualization.Charting;
using System.Windows.Forms;
using static GGo_SIM_v1.Upbit_API_v2_SIM;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TextBox;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TrackBar;
using System.Web.Management;
using System.Globalization;

namespace GGo_SIM_v1
{
    public class Upbit_API_v2_SIM
    {
        private readonly string _accessKey;
        private readonly string _secretKey;
        private string _tradeHistoryPath;
        public Upbit_API_v2_SIM(string AccessKey, string SecretKey)
        {
            _accessKey = AccessKey;
            _secretKey = SecretKey;

        }



        public List<CoinList> CoinListData = new List<CoinList>();



        #region Data Classes


        // 정보 업데이트. (계좌, Ticker, Candle, Orderbook, OrderList)
        public class UpbitData
        {
            public List<CoinList> CoinLists { get; set; }
            public List<Ticker> Tickers { get; set; }
            public CandleData CandleData { get; set; }
        }



        // 코인 리스트
        public class CoinList
        {
            public string CoinName { get; set; }        // 코인명 영문
            public string CoinNameKR { get; set; }      // 코인명 한글
            public bool IsWarning { get; set; }         // 유의 정보
            public string Caution { get; set; }         // 주의 정보
        }

        // Ticker
        public class Ticker
        {
            public string CoinName { get; set; }        // 코인명 영문
            public string CoinNameKR { get; set; }      // 코인명 한글

            public double Opening_Price { get; set; }         // 시가
            public double High_Price { get; set; }             // 고가
            public double Low_Price { get; set; }              // 저가
            public double Trade_Price { get; set; }            // 종가
            public double Prev_Trade_Price { get; set; }       // 전일 종가

            public string Change { get; set; }                // 보합,상승,하락
            public double Change_Price { get; set; }           // 변화액
            public double Change_Rate { get; set; }            // 변화율
            public double Trade_Volume { get; set; }          // 거래량

            public DateTime Datetime { get; set; }             // 타임스탬프


            public double Acc_Trade_Price_24h { get; set; }     // 24시간 누적 거래대금
            public double highest_52_week_price { get; set; }   // 52주 신고가
            public double lowest_52_week_price { get; set; }   // 52주 신저가
        }


        // 캔들 데이터 통합
        public class CandleData
        {
            public List<Candle> Candles { get; set; }

            public ChartArea ChartArea { get; set; }

            public Series candleSeries { get; set; }

            // 볼린저
            public List<Series> bollingerSeries { get; set; }


            // 봉우리, 골
            public List<Candle> Peaks { get; set; }
            public List<Candle> Troughs { get; set; }
            public Series peakSeries { get; set; }
            public Series troughSeries { get; set; }


            // 추세선
            public Series upperTrendSeries { get; set; }
            public Series lowerTrendSeries { get; set; }
            public string TrendPattern { get; set; }

            // 일봉
            public List<Candle> Candles_Day { get; set; }
        }


        // 분봉
        public class Candle
        {
            public string CoinName { get; set; }        // 코인명 영문
            public string CoinNameKR { get; set; }      // 코인명 한글

            public int Minutes { get; set; }            // 몇분봉

            public DateTime Datetime { get; set; }      // 타임스탬프

            public double Opening_Price { get; set; }   // 시가
            public double High_Price { get; set; }      // 고가
            public double Low_Price { get; set; }       // 저가
            public double Trade_Price { get; set; }     // 종가

            public double MA { get; set; }                 // 이동평균선
            public double Bollinger_Upper { get; set; }     // 볼린저 상단
            public double Bollinger_Middle { get; set; }    // 볼린저 중단
            public double Bollinger_Lower { get; set; }     // 볼린저 하단

            public string Red_or_Blue { get; set; }         // 양봉 음봉 여부

            // 봉우리, 골
            public double Peak { get; set; }
            public double Trough { get; set; }

            // 추세선
            public double UpperTrendLine { get; set; }  // 상단 추세선 값
            public double LowerTrendLine { get; set; }  // 하단 추세선 값
        }





        #endregion








        #region API Methods

        // Accounts Authorization Token 생성
        private string Get_AuthorizationToken(Dictionary<string, string> queryParams = null)
        {
            string tempQueryHash = "";

            // Query Parameter를 SHA512로 해싱
            if (queryParams != null && queryParams.Count > 0)
            {
                // Query Parameter를 정렬하여 문자열 생성 (key=value&key2=value2)
                var sortedQuery = string.Join("&", queryParams
                    .OrderBy(p => p.Key) // 키 정렬
                    .Select(p => $"{p.Key}={p.Value}"));

                tempQueryHash = GetSha512Hash(sortedQuery); // 해싱
            }

            var payload = new JwtPayload
    {
        { "access_key", _accessKey },
        { "nonce", Guid.NewGuid().ToString() },
        { "query_hash", tempQueryHash },
        { "query_hash_alg", "SHA512" }
    };

            byte[] keyBytes = Encoding.Default.GetBytes(_secretKey);
            var securityKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(keyBytes);
            var credentials = new Microsoft.IdentityModel.Tokens.SigningCredentials(securityKey, "HS256");
            var header = new JwtHeader(credentials);
            var secToken = new JwtSecurityToken(header, payload);
            var jwtToken = new JwtSecurityTokenHandler().WriteToken(secToken);
            return "Bearer " + jwtToken;
        }

        // Query Hash 계산 함수
        private static string GetSha512Hash(string input)
        {
            using (SHA512 sha512 = SHA512.Create())
            {
                byte[] bytes = Encoding.UTF8.GetBytes(input);
                byte[] hash = sha512.ComputeHash(bytes);
                return BitConverter.ToString(hash).Replace("-", "").ToLower();
            }
        }




        // 코인(마켓)리스트 가져오기
        public async Task<List<CoinList>> Get_CoinList_API()
        {
            // Query Parameter
            var queryParams = new Dictionary<string, string>
            {
                { "is_details", "true" }
            };

            string authorizationToken = Get_AuthorizationToken(queryParams);

            var client = new RestClient("https://api.upbit.com/v1/market/all?is_details=true");
            var request = new RestRequest();
            request.Method = Method.Get;
            request.AddHeader("Authorization", authorizationToken);

            var response = await client.ExecuteAsync(request);

            if (response.IsSuccessful)
            {
                // JSON 데이터를 List<Upbit_Market> 형식으로 역직렬화
                List<Upbit_Market> allCoins = JsonConvert.DeserializeObject<List<Upbit_Market>>(response.Content);

                // 코인 리스트에 저장
                CoinListData = allCoins
                    .Where(coin => coin.Name_Market.StartsWith("KRW-")) // KRW-로 시작하는 항목만 필터링
                    .Select(coin => new CoinList
                    {
                        CoinName = coin.Name_Market.Replace("KRW-", ""), // KRW- 제거
                        CoinNameKR = coin.Name_KR,
                        IsWarning = coin.Market_Event.Warning,
                        Caution = coin.Market_Event.Caution.PriceFluctuations == true ? "가격 급등락" :
                                    coin.Market_Event.Caution.TradingVolumeSoaring == true ? "거래량 급등" :
                                    coin.Market_Event.Caution.DepositAmountSoaring == true ? "입급량 급등" :
                                    coin.Market_Event.Caution.GlobalPriceDifferences == true ? "글로벌 시세 차이" :
                                    coin.Market_Event.Caution.ConcentrationOfSmallAccounts == true ? "소수 계정 집중" : ""
                    })
                    .OrderBy(coin => coin.CoinNameKR) // Name_KR 기준으로 정렬
                    .ToList();

                return CoinListData.ToList();
            }
            else
            {
                Console.WriteLine($"API 요청 실패: {response.StatusCode} - {response.Content}");
                return null;
            }
        }


        public async Task<CandleData> Get_Candle_Minutes_API(string CoinName, int Minute, int N_of_Candles)
        {
            // ---- 누적 수집 (페이지네이션) ----
            var collected = new List<Upbit_Candle_Minutes>();
            var seen = new HashSet<string>(); // 중복 방지(utc 문자열 기준)

            string market = $"KRW-{CoinName}";
            string toKst = null; // 다음 페이지 기준(KST, exclusive, 반드시 +09:00 포함)
            int need = Math.Min(N_of_Candles + 19, 2000); // MA20 여유 + 상한(원하면 조절)
            string lastCursor = null; // 무진행 루프 방지

            while (collected.Count < need)
            {
                int take = Math.Min(200, need - collected.Count);

                var queryParams = new Dictionary<string, string>
        {
            { "market", market },
            { "count",  take.ToString() }
        };
                if (!string.IsNullOrEmpty(toKst))
                    queryParams["to"] = toKst; // KST(+09:00), exclusive

                // 무진행 루프 방지: 같은 커서면 강제로 더 과거로 이동
                if (queryParams.TryGetValue("to", out var cur) && cur == lastCursor)
                {
                    // 1분 더 과거로 밀기
                    var off = DateTimeOffset.ParseExact(cur, "yyyy-MM-dd'T'HH:mm:ssK", CultureInfo.InvariantCulture);
                    toKst = off.AddMinutes(-1).ToString("yyyy-MM-dd'T'HH:mm:ssK");
                    queryParams["to"] = toKst;
                }
                lastCursor = queryParams.ContainsKey("to") ? queryParams["to"] : null;

                string authorizationToken = Get_AuthorizationToken(queryParams);

                var client = new RestClient($"https://api.upbit.com/v1/candles/minutes/{Minute}");
                var request = new RestRequest { Method = Method.Get };
                foreach (var p in queryParams) request.AddQueryParameter(p.Key, p.Value);
                request.AddHeader("Authorization", authorizationToken);

                var response = await client.ExecuteAsync(request);
                if (!response.IsSuccessful || string.IsNullOrEmpty(response.Content))
                    break; // 멈추지 말고 종료

                var page = JsonConvert.DeserializeObject<List<Upbit_Candle_Minutes>>(response.Content);
                if (page == null || page.Count == 0)
                    break;

                int before = collected.Count;

                // 최신→과거 내림차순으로 옴. 중복 제거 후 추가
                foreach (var c in page)
                {
                    // UTC 문자열 고유키 사용
                    if (!string.IsNullOrEmpty(c.candle_date_time_utc) && seen.Add(c.candle_date_time_utc))
                        collected.Add(c);
                }

                // 다음 요청용 to : 가장 오래된 KST - 1초 (exclusive 보장)
                // page.Last().candle_date_time_kst 형식: "yyyy-MM-dd'T'HH:mm:ss"
                string oldestKst = page.Last().candle_date_time_kst;
                var dtOldestKst = DateTime.ParseExact(oldestKst, "yyyy-MM-dd'T'HH:mm:ss", CultureInfo.InvariantCulture);
                var cursorKst = new DateTimeOffset(dtOldestKst, TimeSpan.FromHours(9)).AddSeconds(-1);
                toKst = cursorKst.ToString("yyyy-MM-dd'T'HH:mm:ssK"); // 예: 2025-08-16T10:24:59+09:00

                // 새로 추가된 게 없거나 마지막 페이지로 추정되면 종료
                if (collected.Count == before || page.Count < take) break;

                await Task.Delay(120); // 레이트리밋 여유
            }

            // ---- 변환/계산 ----
            var data = new CandleData { Candles = new List<Candle>() };
            if (collected.Count == 0) return data;

            // 오래된 순 정렬(UTC 기준 문자열)
            collected = collected
                .OrderBy(c => c.candle_date_time_utc, StringComparer.Ordinal)
                .ToList();

            string coinNameKR = CoinListData.FirstOrDefault(a => a.CoinName == CoinName)?.CoinNameKR;

            var candle_ret = collected.Select(a => new Candle
            {
                CoinName = CoinName,
                CoinNameKR = coinNameKR,
                Minutes = a.unit,
                Datetime = DateTime.ParseExact(a.candle_date_time_kst, "yyyy-MM-dd'T'HH:mm:ss", CultureInfo.InvariantCulture),
                Opening_Price = a.opening_price,
                High_Price = a.high_price,
                Low_Price = a.low_price,
                Trade_Price = a.trade_price,
            }).ToList();

            // MA20/볼린저
            if (candle_ret.Count >= 20)
            {
                for (int i = 19; i < candle_ret.Count; i++)
                {
                    var subset = candle_ret.Skip(i + 1 - 20).Take(20);
                    double ma = subset.Average(c => c.Trade_Price);
                    double sd = Math.Sqrt(subset.Average(c => Math.Pow(c.Trade_Price - ma, 2)));

                    candle_ret[i].MA = ma;
                    candle_ret[i].Bollinger_Upper = ma + 2 * sd;
                    candle_ret[i].Bollinger_Middle = ma;
                    candle_ret[i].Bollinger_Lower = ma - 2 * sd;

                    candle_ret[i].Red_or_Blue =
                        candle_ret[i].Trade_Price > candle_ret[i].Opening_Price ? "Red" :
                        candle_ret[i].Trade_Price < candle_ret[i].Opening_Price ? "Blue" : "Doge";

                }

                // 앞 19개(미계산분) 제거
                candle_ret = candle_ret.Skip(19).ToList();
            }
            // 20개 미만이면 MA/볼린저 없이 있는 만큼만 반환

            // 요청 개수로 컷
            if (candle_ret.Count > N_of_Candles)
                candle_ret = candle_ret.Skip(candle_ret.Count - N_of_Candles).Take(N_of_Candles).ToList();

            data.Candles = candle_ret.ToList();


            // ---- 차트 (있을 때만) ----
            if (candle_ret.Count > 0)
            {
                try
                {
                    var chartArea = new ChartArea("MinuteCandle");
                    chartArea.AxisX.MajorGrid.LineColor = Color.FromArgb(25, Color.Black);
                    chartArea.AxisY.MajorGrid.LineColor = Color.FromArgb(25, Color.Black);
                    chartArea.AxisY.Minimum = candle_ret.Min(c => c.Low_Price) * 0.99;
                    chartArea.AxisY.Maximum = candle_ret.Max(c => c.High_Price) * 1.01;
                    chartArea.AxisX.LabelStyle.Format = "HH:mm";
                    chartArea.AxisY.IsStartedFromZero = false;

                    data.candleSeries = Create_CandleSeries_from_List(candle_ret);
                    data.bollingerSeries = Create_BollingerSeries_from_List(candle_ret).ToList();
                    data.ChartArea = chartArea;
                }
                catch
                {
                    // 차트 실패해도 Candles는 그대로 반환
                }
            }

            return data;
        }



        // Upbit 일봉 DTO (분봉용을 재사용 중이면 unit 필드 등 제거 추천)
        public class Upbit_Candle_Days
        {
            public string market { get; set; }
            public string candle_date_time_utc { get; set; } // "yyyy-MM-dd'T'HH:mm:ss"
            public string candle_date_time_kst { get; set; }
            public double opening_price { get; set; }
            public double high_price { get; set; }
            public double low_price { get; set; }
            public double trade_price { get; set; }
            public long timestamp { get; set; }
            public double candle_acc_trade_price { get; set; }
            public double candle_acc_trade_volume { get; set; }
        }

        public async Task<CandleData> Get_Candle_Days_API(string CoinName, int N_of_Candles)
        {
            var collected = new List<Upbit_Candle_Days>();
            var seen = new HashSet<string>();              // 중복 방지(utc 문자열 기준)

            string market = $"KRW-{CoinName}";
            string toKst = null;                           // 다음 페이지 기준(KST, exclusive)
            int need = Math.Min(N_of_Candles + 19, 2000);  // MA20 여유 + 상한

            while (collected.Count < need)
            {
                int take = Math.Min(200, need - collected.Count);

                var queryParams = new Dictionary<string, string>
        {
            { "market", market },
            { "count",  take.ToString() }
        };
                if (!string.IsNullOrEmpty(toKst))
                    queryParams["to"] = toKst; // KST, exclusive

                string authorizationToken = Get_AuthorizationToken(queryParams);

                var client = new RestClient("https://api.upbit.com/v1/candles/days");
                var request = new RestRequest { Method = Method.Get };
                foreach (var p in queryParams) request.AddQueryParameter(p.Key, p.Value);
                request.AddHeader("Authorization", authorizationToken);

                var response = await client.ExecuteAsync(request);
                if (!response.IsSuccessful) break;                     // 멈추지 말고 탈출

                var page = JsonConvert.DeserializeObject<List<Upbit_Candle_Days>>(response.Content);
                if (page == null || page.Count == 1) break;

                // 최신→과거 순으로 옴. 중복 제거 후 추가
                foreach (var c in page)
                    if (seen.Add(c.candle_date_time_utc))
                        collected.Add(c);

                // 다음 요청용 to : 가장 오래된 KST - 1초 (exclusive 보장)
                var oldestKst = page.Last().candle_date_time_kst;                      // "yyyy-MM-dd'T'HH:mm:ss"
                var dtOldestKst = DateTime.ParseExact(oldestKst, "yyyy-MM-dd'T'HH:mm:ss", null);
                toKst = dtOldestKst.AddSeconds(-1).ToString("yyyy-MM-dd HH:mm:ss");    // KST 포맷

                // 마지막 페이지면 종료
                if (page.Count < take) break;

                await Task.Delay(120); // 레이트리밋 여유
            }

            if (collected.Count == 0)
                return new CandleData { Candles = new List<Candle>() };

            // 오래된 순으로 정렬
            collected = collected.OrderBy(c => c.candle_date_time_utc).ToList();

            string coinNameKR = CoinListData.FirstOrDefault(a => a.CoinName == CoinName)?.CoinNameKR;

            var candle_ret = collected.Select(a => new Candle
            {
                CoinName = CoinName,
                CoinNameKR = coinNameKR,
                Minutes = 60 * 24,
                Datetime = DateTime.ParseExact(a.candle_date_time_kst, "yyyy-MM-dd'T'HH:mm:ss", null),
                Opening_Price = a.opening_price,
                High_Price = a.high_price,
                Low_Price = a.low_price,
                Trade_Price = a.trade_price,
            }).ToList();

            // MA20 / 볼린저 (20개 이상일 때만)
            if (candle_ret.Count >= 20)
            {
                for (int i = 19; i < candle_ret.Count; i++)
                {
                    var subset = candle_ret.Skip(i + 1 - 20).Take(20);
                    double ma = subset.Average(c => c.Trade_Price);
                    double sd = Math.Sqrt(subset.Average(c => Math.Pow(c.Trade_Price - ma, 2)));

                    candle_ret[i].MA = ma;
                    candle_ret[i].Bollinger_Upper = ma + 2 * sd;
                    candle_ret[i].Bollinger_Middle = ma;
                    candle_ret[i].Bollinger_Lower = ma - 2 * sd;

                    candle_ret[i].Red_or_Blue =
                        candle_ret[i].Trade_Price > candle_ret[i].Opening_Price ? "Red" :
                        candle_ret[i].Trade_Price < candle_ret[i].Opening_Price ? "Blue" : "Doge";
                }

                // MA 미계산분 제거(앞 19개) 후 요청 개수로 컷
                candle_ret = candle_ret.Skip(19).ToList();
            }
            else
            {
                // 20개 미만이면 MA/볼린저 없이 있는 만큼만 반환
                // 필요 시 여기서 바로 return 해도 됨
            }

            if (candle_ret.Count > N_of_Candles)
                candle_ret = candle_ret.Skip(candle_ret.Count - N_of_Candles).Take(N_of_Candles).ToList();

            // 차트 생성(빈 리스트면 축 계산 시 예외 날 수 있으니 가드)
            var data = new CandleData { Candles = candle_ret.ToList() };

            if (candle_ret.Count > 0)
            {
                try
                {
                    var chartArea = new ChartArea("DayCandle");
                    chartArea.AxisX.MajorGrid.LineColor = Color.FromArgb(25, Color.Black);
                    chartArea.AxisY.MajorGrid.LineColor = Color.FromArgb(25, Color.Black);
                    chartArea.AxisY.Minimum = candle_ret.Min(c => c.Low_Price) * 0.99;
                    chartArea.AxisY.Maximum = candle_ret.Max(c => c.High_Price) * 1.01;
                    chartArea.AxisX.LabelStyle.Format = "MM-dd";
                    chartArea.AxisY.IsStartedFromZero = false;

                    data.candleSeries = Create_CandleSeries_from_List(candle_ret);
                    data.bollingerSeries = Create_BollingerSeries_from_List(candle_ret).ToList();
                    data.ChartArea = chartArea;
                }
                catch
                {
                    // 차트 생성 실패 시 Candles만 채워서 반환
                }
            }

            return data;
        }





        #endregion




        #region UpbitAPI Response Classes

        // 코인(마켓) 리스트 (거래 가능 코인명 리스트)
        public class Upbit_Market
        {
            [JsonProperty("market")]
            public string Name_Market { get; set; }

            [JsonProperty("korean_name")]
            public string Name_KR { get; set; }

            [JsonProperty("english_name")]
            public string Name_ENG { get; set; }

            [JsonProperty("market_event")]
            public MarketEvent Market_Event { get; set; }
        }

        public class MarketEvent
        {
            [JsonProperty("warning")]
            public bool Warning { get; set; }

            [JsonProperty("caution")]
            public MarketCaution Caution { get; set; }
        }

        public class MarketCaution
        {
            [JsonProperty("PRICE_FLUCTUATIONS")]
            public bool PriceFluctuations { get; set; }

            [JsonProperty("TRADING_VOLUME_SOARING")]
            public bool TradingVolumeSoaring { get; set; }

            [JsonProperty("DEPOSIT_AMOUNT_SOARING")]
            public bool DepositAmountSoaring { get; set; }

            [JsonProperty("GLOBAL_PRICE_DIFFERENCES")]
            public bool GlobalPriceDifferences { get; set; }

            [JsonProperty("CONCENTRATION_OF_SMALL_ACCOUNTS")]
            public bool ConcentrationOfSmallAccounts { get; set; }
        }


        // Ticker 조회
        private class Upbit_Ticker
        {
            [JsonProperty("market")]
            public string Market { get; set; } = string.Empty; // 종목 구분 코드

            [JsonProperty("trade_date")]
            public string TradeDateUTC { get; set; } = string.Empty; // 최근 거래 일자 (UTC)

            [JsonProperty("trade_time")]
            public string TradeTimeUTC { get; set; } = string.Empty;// 최근 거래 시간 (UTC)

            [JsonProperty("trade_date_kst")]
            public string TradeDateKST { get; set; } = string.Empty; // 최근 거래 일자 (KST)

            [JsonProperty("trade_time_kst")]
            public string TradeTimeKST { get; set; } = string.Empty; // 최근 거래 시간 (KST)

            [JsonProperty("trade_timestamp")]
            public long TradeTimestamp { get; set; } = 0; // 최근 거래 일시 (UTC, Unix Timestamp)

            [JsonProperty("opening_price")]
            public double OpeningPrice { get; set; } = 0.0; // 시가

            [JsonProperty("high_price")]
            public double HighPrice { get; set; } = 0.0; // 고가

            [JsonProperty("low_price")]
            public double LowPrice { get; set; } = 0.0; // 저가

            [JsonProperty("trade_price")]
            public double TradePrice { get; set; } = 0.0; // 현재가

            [JsonProperty("prev_closing_price")]
            public double PrevClosingPrice { get; set; } = 0.0; // 전일 종가

            [JsonProperty("change")]
            public string Change { get; set; } = string.Empty; // 변동 상태 (EVEN, RISE, FALL)

            [JsonProperty("change_price")]
            public double ChangePrice { get; set; } = 0.0;// 변동액의 절대값

            [JsonProperty("change_rate")]
            public double ChangeRate { get; set; } = 0.0;// 변동률의 절대값

            [JsonProperty("signed_change_price")]
            public double SignedChangePrice { get; set; } = 0.0;// 부호가 있는 변동액

            [JsonProperty("signed_change_rate")]
            public double SignedChangeRate { get; set; } = 0.0;// 부호가 있는 변동률

            [JsonProperty("trade_volume")]
            public double TradeVolume { get; set; } = 0.0;// 가장 최근 거래량

            [JsonProperty("acc_trade_price")]
            public double AccTradePrice { get; set; } = 0.0;// 누적 거래대금 (UTC 0시 기준)

            [JsonProperty("acc_trade_price_24h")]
            public double AccTradePrice24h { get; set; } = 0.0; // 24시간 누적 거래대금

            [JsonProperty("acc_trade_volume")]
            public double AccTradeVolume { get; set; } = 0.0;// 누적 거래량 (UTC 0시 기준)

            [JsonProperty("acc_trade_volume_24h")]
            public double AccTradeVolume24h { get; set; } = 0.0; // 24시간 누적 거래량

            [JsonProperty("highest_52_week_price")]
            public double Highest52WeekPrice { get; set; } = 0.0;// 52주 신고가

            [JsonProperty("highest_52_week_date")]
            public string Highest52WeekDate { get; set; } = string.Empty;// 52주 신고가 달성일

            [JsonProperty("lowest_52_week_price")]
            public double Lowest52WeekPrice { get; set; } = 0.0;// 52주 신저가

            [JsonProperty("lowest_52_week_date")]
            public string Lowest52WeekDate { get; set; } = string.Empty; // 52주 신저가 달성일

            [JsonProperty("timestamp")]
            public long Timestamp { get; set; } = 0; // 타임스탬프
        }


        // 분 캔들 조회
        private class Upbit_Candle_Minutes
        {
            public string market { get; set; }
            public string candle_date_time_utc { get; set; }
            public string candle_date_time_kst { get; set; }
            public double opening_price { get; set; }
            public double high_price { get; set; }
            public double low_price { get; set; }
            public double trade_price { get; set; }
            public long timestamp { get; set; }
            public double candle_acc_trade_price { get; set; }
            public double candle_acc_trade_volume { get; set; }
            public int unit { get; set; }
        }




        #endregion






        #region Events



        // 정보 업데이트
        public event EventHandler<UpbitDataEventArgs> UpbitDataRetrieved;
        public class UpbitDataEventArgs : EventArgs
        {
            public UpbitData UpbitData { get; set; }

            public UpbitDataEventArgs(UpbitData upbitData)
            {
                UpbitData = upbitData;
            }
        }



        // 주문 완료 이벤트
        public event EventHandler<OrderEventArgs> OrderRetrieved;
        public class OrderEventArgs : EventArgs
        {
            public string Message { get; }

            public OrderEventArgs(string message)
            {
                Message = message;
            }
        }




        #endregion









        #region Functions




        // List<Candle> to Candle Chart Series
        public Series Create_CandleSeries_from_List(List<Candle> candles)
        {
            var tempCandles = candles.ToList();

            Series _series = new Series("캔들");
            _series.ChartType = SeriesChartType.Candlestick;
            _series.XValueType = ChartValueType.DateTime;
            _series["OpenCloseStyle"] = "Triangle";
            _series["ShowOpenClose"] = "Both";
            _series.IsXValueIndexed = false;
            _series.IsVisibleInLegend = false;

            foreach (var candle in tempCandles)
            {
                var index = _series.Points.AddXY(candle.Datetime, candle.Trade_Price); // X축 값과 초기 Y값 추가

                // 캔들 차트의 시가, 종가, 저가, 고가 설정
                _series.Points[index].YValues = new double[4];
                _series.Points[index].YValues[0] = candle.Low_Price;      // Low
                _series.Points[index].YValues[1] = candle.High_Price;     // High
                _series.Points[index].YValues[2] = candle.Opening_Price;  // Open
                _series.Points[index].YValues[3] = candle.Trade_Price;    // Close
                _series.Points[index].Color = candle.Red_or_Blue == "Red" ? Color.Red : Color.Blue;
            }

            // 양봉(빨강)과 음봉(파랑) 색상 설정
            _series["PriceUpColor"] = "Red";   // 상승(양봉) 색상
            _series["PriceDownColor"] = "Blue"; // 하락(음봉) 색상

            return _series;
        }


        // List<Candle> to Bollinger Series
        public List<Series> Create_BollingerSeries_from_List(List<Candle> candles)
        {
            List<Series> series_ret = new List<Series>();

            var tempCandles = candles.ToList();

            Series series_bollinger_upper = new Series("볼린저 상단");
            series_bollinger_upper.ChartType = SeriesChartType.Line;
            series_bollinger_upper.XValueType = ChartValueType.DateTime;
            series_bollinger_upper.Color = Color.Blue;
            series_bollinger_upper.IsVisibleInLegend = false;

            Series series_bollinger_mid = new Series("볼린저 중단");
            series_bollinger_mid.ChartType = SeriesChartType.Line;
            series_bollinger_mid.XValueType = ChartValueType.DateTime;
            series_bollinger_mid.Color = Color.Green;
            series_bollinger_mid.IsVisibleInLegend = false;

            Series series_bollinger_lower = new Series("볼린저 하단");
            series_bollinger_lower.ChartType = SeriesChartType.Line;
            series_bollinger_lower.XValueType = ChartValueType.DateTime;
            series_bollinger_lower.Color = Color.Gold;
            series_bollinger_lower.IsVisibleInLegend = false;


            foreach (var candle in tempCandles)
            {
                // 볼린저 밴드
                series_bollinger_upper.Points.AddXY(candle.Datetime, candle.Bollinger_Upper);
                series_bollinger_mid.Points.AddXY(candle.Datetime, candle.Bollinger_Middle);
                series_bollinger_lower.Points.AddXY(candle.Datetime, candle.Bollinger_Lower);
            }

            series_ret.Add(series_bollinger_upper);
            series_ret.Add(series_bollinger_mid);
            series_ret.Add(series_bollinger_lower);

            return series_ret;
        }


        // List<Candle> to Peak n Trough Series
        public Series Create_PeakNtroughSeries(string SeriesName)
        {
            Series _series = new Series(SeriesName);
            _series.ChartType = SeriesChartType.Line;
            _series.XValueType = ChartValueType.DateTime;
            _series.Color = Color.Black;
            _series.IsVisibleInLegend = false;

            return _series;
        }






        #endregion




    }
}
