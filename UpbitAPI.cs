using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IdentityModel.Tokens.Jwt;
using Newtonsoft.Json;
using RestSharp;
using Microsoft.IdentityModel.Tokens;
using System.Security.Principal;
using System.Security.Cryptography;

namespace ThisIsIt_v2
{
    public class UpbitApi
    {
        private readonly string _accessKey;
        private readonly string _secretKey;

        public UpbitApi(string accessKey, string secretKey)
        {
            _accessKey = accessKey;
            _secretKey = secretKey;
        }
        // JWT 토큰 생성 메서드
        private string GenerateJwtToken()
        {
            var payload = new JwtPayload
        {
            { "access_key", _accessKey },
            { "nonce", Guid.NewGuid().ToString() }
        };

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
            var header = new JwtHeader(credentials);
            var token = new JwtSecurityToken(header, payload);
            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        // 전체 계좌 조회 메서드
        public async Task<List<Account>> GetAccountsAsync()
        {
            string jwtToken = GenerateJwtToken();
            string authorizationToken = $"Bearer {jwtToken}";

            var client = new RestClient("https://api.upbit.com/v1/accounts");
            var request = new RestRequest();
            request.Method = Method.Get;
            request.AddHeader("Authorization", authorizationToken);

            var response = await client.ExecuteAsync(request);

            if (response.IsSuccessful)
            {
                return JsonConvert.DeserializeObject<List<Account>>(response.Content);
            }
            else
            {
                Console.WriteLine($"API 요청 실패: {response.StatusCode} - {response.Content}");
                return null;
            }
        }


        // 현재 시세 조회 메서드
        public async Task<decimal> GetCurrentPriceAsync(string market)
        {
            // 원화 마켓으로 고정
            string marketPair = $"KRW-{market}";

            var client = new RestClient($"https://api.upbit.com/v1/ticker?markets={marketPair}");
            var request = new RestRequest();
            request.Method = Method.Get;

            var response = await client.ExecuteAsync(request);
            var data = JsonConvert.DeserializeObject<List<Ticker>>(response.Content);

            return data[0].TradePrice;
        }
        /*
        public async Task<decimal> GetCurrentPriceAsync(string market)
        {
            var client = new RestClient($"https://api.upbit.com/v1/ticker?markets={market}");
            var request = new RestRequest();
            request.Method = Method.Get;
            var response = await client.ExecuteAsync(request);
            var data = JsonConvert.DeserializeObject<List<Ticker>>(response.Content);
            return data[0].TradePrice;
        }
        */

        #region 주문

        // 주문
        public async Task<string> PlaceOrderAsync(string market, string side, string volume, string price, string ordType)
        {
            // API 키 설정
            string accessKey = _accessKey;
            string secretKey = _secretKey;

            // 1. Query 문자열 생성
            var query = $"market={market}&side={side}&volume={volume}&price={price}&ord_type={ordType}";

            // 2. Query Hash 생성 (SHA512)
            string queryHash;
            using (SHA512 sha512 = SHA512.Create())
            {
                byte[] hash = sha512.ComputeHash(Encoding.UTF8.GetBytes(query));
                queryHash = BitConverter.ToString(hash).Replace("-", "").ToLower();
            }

            // 3. JWT 토큰 생성
            var claims = new List<System.Security.Claims.Claim>
            {
                new System.Security.Claims.Claim("access_key", accessKey),
                new System.Security.Claims.Claim("nonce", Guid.NewGuid().ToString()),
                new System.Security.Claims.Claim("query_hash", queryHash),
                new System.Security.Claims.Claim("query_hash_alg", "SHA512")
            };

            var securityKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var credentials = new Microsoft.IdentityModel.Tokens.SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
            var header = new JwtHeader(credentials);
            var payload = new JwtPayload(claims);
            var secToken = new JwtSecurityToken(header, payload);
            var jwtToken = new JwtSecurityTokenHandler().WriteToken(secToken);

            // 4. HTTP 요청 생성
            var client = new RestClient("https://api.upbit.com/v1/orders");
            var request = new RestRequest();
            request.Method = Method.Post;
            request.AddHeader("Authorization", $"Bearer {jwtToken}");
            request.AddHeader("Content-Type", "application/json");

            // 5. 요청 본문 생성 및 추가
            var body = new
            {
                market = market,
                side = side,
                volume = volume,
                price = price,
                ord_type = ordType
            };
            request.AddJsonBody(body);

            // 6. API 요청 및 응답 처리
            var response = await client.ExecuteAsync(request);
            if (response.IsSuccessful)
            {
                Console.WriteLine("주문 요청 성공!");
                return response.Content;
            }
            else
            {
                Console.WriteLine($"주문 요청 실패: {response.StatusCode} - {response.Content}");
                throw new Exception($"API 요청 실패: {response.StatusCode} - {response.Content}");
            }
        }



        #endregion




        // 계좌 정보 클래스
        /*
        public class Account
        {
            public string Currency { get; set; }
            public string Balance { get; set; }
            public string Locked { get; set; }
            public string AvgBuyPrice { get; set; }
            public bool AvgBuyPriceModified { get; set; }
            public string UnitCurrency { get; set; }
        }*/

        public class Account
        {
            [JsonProperty("currency")]
            public string Currency { get; set; }

            [JsonProperty("balance")]
            public string Balance { get; set; }

            [JsonProperty("locked")]
            public string Locked { get; set; }

            [JsonProperty("avg_buy_price")]
            public string AvgBuyPrice { get; set; }

            [JsonProperty("avg_buy_price_modified")]
            public bool AvgBuyPriceModified { get; set; }

            [JsonProperty("unit_currency")]
            public string UnitCurrency { get; set; }
        }

        public class ProfitLossData
        {
            public string Currency { get; set; }
            public string Balance { get; set; }
            public decimal ProfitLoss_Won { get; set; }
            public decimal ProfitLoss_Rate { get; set; }
        }


        // 시세 정보 클래스
        public class Ticker
        {
            [JsonProperty("trade_price")]
            public decimal TradePrice { get; set; }
        }
    }

}
