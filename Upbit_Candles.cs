using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace GGo_v1.UpbitAPI
{
    public class Upbit_Candle
    {
        private readonly string _accessKey;
        private readonly string _secretKey;

        public Upbit_Candle(string accessKey, string secretKey)
        {
            _accessKey = accessKey;
            _secretKey = secretKey;
        }



        #region Data Class


        // 캔들 정보 클래스
        public class Candle
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

            // 볼린저 밴드와 이동평균선 속성 추가
            public int MA { get; set; } // MA 수
            public double MovingAverage { get; set; } // 이동평균선
            public double BollingerUpper { get; set; } // 볼린저 상단
            public double BollingerMiddle { get; set; } // 볼린저 중단
            public double BollingerLower { get; set; } // 볼린저 하단

            // 양봉, 음봉 여부
            public string Red_or_Blue { get; set; }


            // MA 기울기
            public double MA_Slope { get; set; }


            public DateTime? CandleDateTimeUtc
            {
                get
                {
                    if (DateTime.TryParse(candle_date_time_utc, out var date))
                        return date;
                    return null;
                }
            }

            public DateTime? CandleDateTimeKst
            {
                get
                {
                    if (DateTime.TryParse(candle_date_time_kst, out var date))
                        return date;
                    return null;
                }
            }
        }



        #endregion




        #region Authorization Token


        // Authorization Token 생성
        public string Get_AuthorizationToken(Dictionary<string, string> queryParams = null)
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

        #endregion




        #region Public Methods



        // 최근 분캔들 200개 가져오기
        public async Task<List<Candle>> Get_Minute_Candles(string coinName, int unit, int ma_period)
        {
            // Query Parameter
            var queryParams = new Dictionary<string, string>
            {
                { "market", $"KRW-{coinName}" },
                { "count", "200" }
            };

            // Authorization Token 생성
            string authorizationToken = Get_AuthorizationToken(queryParams);

            // RestClient 및 Request 구성
            var client = new RestClient($"https://api.upbit.com/v1/candles/minutes/{unit}");
            var request = new RestRequest();
            request.Method = Method.Get;

            // Query Parameters 추가
            foreach (var param in queryParams)
            {
                request.AddQueryParameter(param.Key, param.Value);
            }

            // Authorization 헤더 추가
            request.AddHeader("Authorization", authorizationToken);

            // 요청 실행
            var response = await client.ExecuteAsync(request);

            // 응답 처리
            if (response.IsSuccessful)
            {
                List<Candle> tempCandles = JsonConvert.DeserializeObject<List<Candle>>(response.Content);

                tempCandles[0].MA = ma_period;

                 Calculate_CandleData(tempCandles);

                return tempCandles;
            }
            else
            {
                Console.WriteLine($"API 요청 실패: {response.StatusCode} - {response.Content}");
                return null;
            }
        }




        #endregion


        #region Private Method


        private void Calculate_CandleData(List<Candle> candles)
        {
            int MA_period = candles[0].MA;

            for (int i = 0; i < candles.Count; i++)
            {
                if (i + 1 >= MA_period)
                {
                    // 이동평균 계산
                    var subset = candles.Skip(i + 1 - MA_period).Take(MA_period);
                    double movingAverage = subset.Average(c => c.trade_price);

                    // 표준편차 계산
                    double standardDeviation = Math.Sqrt(subset.Average(c => Math.Pow(c.trade_price - movingAverage, 2)));

                    // 볼린저 밴드 계산
                    candles[i].MovingAverage = movingAverage;
                    candles[i].BollingerUpper = movingAverage + (2 * standardDeviation);
                    candles[i].BollingerMiddle = movingAverage;
                    candles[i].BollingerLower = movingAverage - (2 * standardDeviation);

                    // 양봉, 음봉 계산
                    candles[i].Red_or_Blue =
                        candles[i].trade_price > candles[i].opening_price ? "Red" :
                        candles[i].trade_price < candles[i].opening_price ? "Blue" : "Doge";

                    // MA 기울기 계산
                    candles[i].MA_Slope = candles[i].BollingerMiddle - candles[i - 1].BollingerMiddle;
                }
            }
        }

        #endregion

    }


}
