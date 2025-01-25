using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web.UI.WebControls;
using static GGo_v1.UpbitAPI.Upbit_Account;
using static GGo_v1.UpbitAPI.Upbit_Candle;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TextBox;

namespace GGo_v1.UpbitAPI
{
    public class Upbit_Order
    {
        private readonly string _accessKey;
        private readonly string _secretKey;

        public Upbit_Order(string accessKey, string secretKey)
        {
            _accessKey = accessKey;
            _secretKey = secretKey;

            Init_ExceptionCoins();
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




        #region Data Class


        // 주문 요청 응답
        public class Order_Response
        {
            [JsonProperty("uuid")]
            public string Uuid { get; set; } // 주문의 고유 아이디

            [JsonProperty("side")]
            public string Side { get; set; } // 주문 종류

            [JsonProperty("ord_type")]
            public string OrderType { get; set; } // 주문 방식

            [JsonProperty("price")]
            public string Price { get; set; } // 주문 당시 화폐 가격 (NumberString)

            [JsonProperty("state")]
            public string State { get; set; } // 주문 상태

            [JsonProperty("market")]
            public string Market { get; set; } // 마켓의 유일키

            [JsonProperty("created_at")]
            public string CreatedAt { get; set; } // 주문 생성 시간

            [JsonProperty("volume")]
            public string Volume { get; set; } // 사용자가 입력한 주문 양 (NumberString)

            [JsonProperty("remaining_volume")]
            public string RemainingVolume { get; set; } // 체결 후 남은 주문 양 (NumberString)

            [JsonProperty("reserved_fee")]
            public string ReservedFee { get; set; } // 수수료로 예약된 비용 (NumberString)

            [JsonProperty("remaining_fee")]
            public string RemainingFee { get; set; } // 남은 수수료 (NumberString)

            [JsonProperty("paid_fee")]
            public string PaidFee { get; set; } // 사용된 수수료 (NumberString)

            [JsonProperty("locked")]
            public string Locked { get; set; } // 거래에 사용중인 비용 (NumberString)

            [JsonProperty("executed_volume")]
            public string ExecutedVolume { get; set; } // 체결된 양 (NumberString)

            [JsonProperty("trades_count")]
            public int TradesCount { get; set; } // 해당 주문에 걸린 체결 수 (Integer)

            [JsonProperty("time_in_force")]
            public string TimeInForce { get; set; } // IOC, FOK 설정

            [JsonProperty("identifier")]
            public string Identifier { get; set; } // 조회용 사용자 지정 값
        }



        // 미체결 주문 클래스
        public class PendingOrder
        {
            [JsonProperty("uuid")]
            public string Uuid { get; set; } // 주문의 고유 아이디

            [JsonProperty("side")]
            public string Side { get; set; } // 주문 종류

            [JsonProperty("ord_type")]
            public string OrderType { get; set; } // 주문 방식

            [JsonProperty("price")]
            public string Price { get; set; } // 주문 당시 화폐 가격 (NumberString)

            [JsonProperty("state")]
            public string State { get; set; } // 주문 상태

            [JsonProperty("market")]
            public string Market { get; set; } // 마켓의 유일키

            [JsonProperty("created_at")]
            public string CreatedAt { get; set; } // 주문 생성 시간

            [JsonProperty("volume")]
            public string Volume { get; set; } // 사용자가 입력한 주문 양 (NumberString)

            [JsonProperty("remaining_volume")]
            public string RemainingVolume { get; set; } // 체결 후 남은 주문 양 (NumberString)

            [JsonProperty("reserved_fee")]
            public string ReservedFee { get; set; } // 수수료로 예약된 비용 (NumberString)

            [JsonProperty("remaining_fee")]
            public string RemainingFee { get; set; } // 남은 수수료 (NumberString)

            [JsonProperty("paid_fee")]
            public string PaidFee { get; set; } // 사용된 수수료 (NumberString)

            [JsonProperty("locked")]
            public string Locked { get; set; } // 거래에 사용 중인 비용 (NumberString)

            [JsonProperty("executed_volume")]
            public string ExecutedVolume { get; set; } // 체결된 양 (NumberString)

            [JsonProperty("trades_count")]
            public int TradesCount { get; set; } // 해당 주문에 걸린 체결 수

            [JsonProperty("time_in_force")]
            public string TimeInForce { get; set; } // IOC, FOK 설정

            [JsonProperty("identifier")]
            public string Identifier { get; set; } // 조회용 사용자 지정 값
        }



        #endregion




        #region 주문

        // 주문
        public async Task<Order_Response> PlaceOrderAsync(string market, string side, string volume, string price, string ordType)
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
                return JsonConvert.DeserializeObject<Order_Response>(response.Content);
            }
            else
            {
                Console.WriteLine($"주문 요청 실패: {response.StatusCode} - {response.Content}");
                //throw new Exception($"API 요청 실패: {response.StatusCode} - {response.Content}");
                return null;
            }
        }
        
        #endregion




        #region Public Method




        // 시장가 매도 주문
        public async Task<Order_Response> Sell_Order_MarketPrice(string coinName, string volume)
        {
            return await PlaceOrderAsync("KRW-" + coinName, "ask", volume, "", "market");
        }


        // 시장가 매수 주문
        public async Task<Order_Response> Buy_Order_MarketPrice(string coinName, string price)
        {
            return await PlaceOrderAsync("KRW-" + coinName, "bid", "", price, "price");

        }


        // 지정가 매도 주문
        public async Task<Order_Response> Sell_Order_LimitPrice(string coinName, string volume, string price)
        {
            // 소수점 맞추기
            decimal adjustedPrice = GetAdjustedLimitPrice(coinName, decimal.Parse(price));

            return await PlaceOrderAsync("KRW-" + coinName, "ask", volume, adjustedPrice.ToString(), "limit");
        }


        // 지정가 매수 주문
        public async Task<Order_Response> Buy_Order_LimitPrice(string coinName, string voluem, string price)
        {
            // 소수점 맞추기
            decimal adjustedPrice = GetAdjustedLimitPrice(coinName, decimal.Parse(price));

            return await PlaceOrderAsync("KRW-" + coinName, "bid", voluem, adjustedPrice.ToString(), "limit");

        }


        // 미체결 내역 조회
        public async Task<List<PendingOrder>> Get_PendingOrders()
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
            string jwtToken = new JwtSecurityTokenHandler().WriteToken(token);

            string authorizationToken = $"Bearer {jwtToken}";

            var client = new RestClient("https://api.upbit.com/v1/orders");
            var request = new RestRequest();
            request.Method = Method.Get;
            request.AddHeader("Authorization", authorizationToken);

            var response = await client.ExecuteAsync(request);

            if (response.IsSuccessful)
            {
                // JSON 데이터를 객체로 변환
                var orders = JsonConvert.DeserializeObject<List<PendingOrder>>(response.Content);

                // Market 속성에서 "KRW-" 제거
                foreach (var order in orders)
                {
                    if (order.Market.StartsWith("KRW-"))
                    {
                        order.Market = order.Market.Replace("KRW-", string.Empty);
                    }
                }

                return orders;
            }
            else
            {
                Console.WriteLine($"API 요청 실패: {response.StatusCode} - {response.Content}");
                return null;
            }

        }


      



        // 미체결 거래 취소
        public async Task<PendingOrder> Delete_PendingOrder(string uuid)
        {
            string queryString = $"uuid={uuid}";


            // JWT 토큰 생성
            // 1. Query Hash 생성 (SHA-512 해시)
            string queryHash;
            using (var sha512 = SHA512.Create())
            {
                var hashBytes = sha512.ComputeHash(Encoding.UTF8.GetBytes(queryString));
                queryHash = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
            }

            // 2. JWT Payload 생성
            var payload = new JwtPayload
    {
        { "access_key", _accessKey }, // API 키
        { "nonce", Guid.NewGuid().ToString() }, // 요청 고유값
        { "query_hash", queryHash }, // Query String의 SHA-512 해시값
        { "query_hash_alg", "SHA512" } // 해시 알고리즘 이름
    };

            // 3. Secret Key 설정
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            // 4. JWT Header와 Token 생성
            var header = new JwtHeader(credentials);
            var token = new JwtSecurityToken(header, payload);

            // 5. JWT Token 반환
            var jwtToken = new JwtSecurityTokenHandler().WriteToken(token);


            // HTTP 요청
            var client = new RestClient("https://api.upbit.com/v1/order");
            var request = new RestRequest();
            request.Method = Method.Delete;
            request.AddHeader("Authorization", $"Bearer {jwtToken}");
            request.AddQueryParameter("uuid", uuid);

            var response = await client.ExecuteAsync(request);


            if (response.IsSuccessful)
            {
                // JSON 데이터를 객체로 변환
                var orders = JsonConvert.DeserializeObject<PendingOrder>(response.Content);

                // Market 속성에서 "KRW-" 제거
                if (orders.Market.StartsWith("KRW-"))
                {
                    orders.Market = orders.Market.Replace("KRW-", string.Empty);
                }

                return orders;
            }
            else
            {
                Console.WriteLine($"API 요청 실패: {response.StatusCode} - {response.Content}");
                return null;
            }

        }


        #endregion




        #region 매도 소수점 맞추기

        List<string> _ExceptionCoins = new List<string>();


        // 예외처리 코인 리스트
        void Init_ExceptionCoins()
        {
            _ExceptionCoins.Add("ADA");
            _ExceptionCoins.Add("ALGO");
            _ExceptionCoins.Add("BLUR");
            _ExceptionCoins.Add("CELO");
            _ExceptionCoins.Add("ELF");
            _ExceptionCoins.Add("EOS");
            _ExceptionCoins.Add("GRS");
            _ExceptionCoins.Add("GRT");
            _ExceptionCoins.Add("ICX");
            _ExceptionCoins.Add("MANA");
            _ExceptionCoins.Add("MINA");
            _ExceptionCoins.Add("POL");
            _ExceptionCoins.Add("SAND");
            _ExceptionCoins.Add("SEI");
            _ExceptionCoins.Add("STG");
            _ExceptionCoins.Add("TRX");
        }

        // 지정가 소수점 맞추기
        public decimal GetAdjustedLimitPrice(string CoinName, decimal Sell_Price)
        {

            // 주문 가격 단위에 따라 매도 가격 조정
            decimal adjustedSellPrice = 0;

            // 예외 코인인지 확인
            if (_ExceptionCoins.Contains(CoinName))
            {
                adjustedSellPrice = Math.Floor(Sell_Price / 1) * 1; // 1 KRW 단위
            }
            else if (Sell_Price >= 2000000)
            {
                adjustedSellPrice = Math.Floor(Sell_Price / 1000) * 1000; // 1,000 KRW 단위
            }
            else if (Sell_Price >= 1000000)
            {
                adjustedSellPrice = Math.Floor(Sell_Price / 500) * 500; // 500 KRW 단위
            }
            else if (Sell_Price >= 500000)
            {
                adjustedSellPrice = Math.Floor(Sell_Price / 100) * 100; // 100 KRW 단위
            }
            else if (Sell_Price >= 100000)
            {
                adjustedSellPrice = Math.Floor(Sell_Price / 50) * 50; // 50 KRW 단위
            }
            else if (Sell_Price >= 10000)
            {
                adjustedSellPrice = Math.Floor(Sell_Price / 10) * 10; // 10 KRW 단위
            }
            else if (Sell_Price >= 1000)
            {
                adjustedSellPrice = Math.Floor(Sell_Price / 1) * 1; // 1 KRW 단위
            }
            else if (Sell_Price >= 100)
            {
                adjustedSellPrice = Math.Floor(Sell_Price / 0.1m) * 0.1m; // 0.1 KRW 단위
            }
            else if (Sell_Price >= 10)
            {
                adjustedSellPrice = Math.Floor(Sell_Price / 0.01m) * 0.01m; // 0.01 KRW 단위
            }
            else if (Sell_Price >= 1)
            {
                adjustedSellPrice = Math.Floor(Sell_Price / 0.001m) * 0.001m; // 0.001 KRW 단위
            }
            else if (Sell_Price >= 0.1m)
            {
                adjustedSellPrice = Math.Floor(Sell_Price / 0.0001m) * 0.0001m; // 0.0001 KRW 단위
            }
            else if (Sell_Price >= 0.01m)
            {
                adjustedSellPrice = Math.Floor(Sell_Price / 0.00001m) * 0.00001m; // 0.00001 KRW 단위
            }
            else if (Sell_Price >= 0.001m)
            {
                adjustedSellPrice = Math.Floor(Sell_Price / 0.000001m) * 0.000001m; // 0.000001 KRW 단위
            }
            else
            {
                adjustedSellPrice = Math.Floor(Sell_Price / 0.0000001m) * 0.0000001m; // 0.0000001 KRW 단위
            }

            return adjustedSellPrice;
        }


        #endregion


    }
}
