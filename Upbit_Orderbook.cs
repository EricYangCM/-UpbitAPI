using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using static GGo_v1.UpbitAPI.Upbit_Account;
using static GGo_v1.UpbitAPI.Upbit_Orderbook;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TextBox;

namespace GGo_v1.UpbitAPI
{
    public class Upbit_Orderbook
    {
        private readonly string _accessKey;
        private readonly string _secretKey;

        public Upbit_Orderbook(string accessKey, string secretKey)
        {
            _accessKey = accessKey;
            _secretKey = secretKey;

        }


        // Orderbook 클래스 정의
        public class Orderbook
        {
            [JsonProperty("market")]
            public string Market { get; set; } // 종목 코드

            [JsonProperty("timestamp")]
            public long Timestamp { get; set; } // 호가 생성 시각

            [JsonProperty("total_ask_size")]
            public double TotalAskSize { get; set; } // 호가 매도 총 잔량

            [JsonProperty("total_bid_size")]
            public double TotalBidSize { get; set; } // 호가 매수 총 잔량

            [JsonProperty("orderbook_units")]
            public List<OrderbookUnit> OrderbookUnits { get; set; } // 호가 리스트
        }

        // OrderbookUnit 클래스 정의
        public class OrderbookUnit
        {
            [JsonProperty("ask_price")]
            public double AskPrice { get; set; } // 매도 호가

            [JsonProperty("bid_price")]
            public double BidPrice { get; set; } // 매수 호가

            [JsonProperty("ask_size")]
            public double AskSize { get; set; } // 매도 잔량

            [JsonProperty("bid_size")]
            public double BidSize { get; set; } // 매수 잔량
        }





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



        // 호가 조회
        public async Task<Orderbook> Get_Orderbook(string market)
        {
            string apiUrl = $"https://api.upbit.com/v1/orderbook?markets={"KRW-" + market}";

            using (var client = new HttpClient())
            {
                var response = await client.GetAsync(apiUrl);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();

                    // JSON 응답을 명확한 모델로 역직렬화
                    var orderBooks = JsonConvert.DeserializeObject<List<Orderbook>>(content);

                    return orderBooks[0];
                }
                else
                {
                    Console.WriteLine($"API 요청 실패: {response.StatusCode}");
                    return null;
                }
            }
        }

        #endregion


    }

}
