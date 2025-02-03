using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.UI.WebControls;
using static GGo_v2.Upbit_API_v2;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TextBox;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TrackBar;

namespace GGo_v2
{
    public class Upbit_API_v2
    {
        private readonly string _accessKey;
        private readonly string _secretKey;
        public Upbit_API_v2(string AccessKey,  string SecretKey)
        {
            _accessKey = AccessKey;
            _secretKey = SecretKey;

            // 명령 처리 Task 실행
            Task.Run(ProcessCommandsAsync);

            // 마켓 리스트 조회
            Request_MarketList();

            // Ticker 조회
            Request_Ticker();

            // 데이터 업데이터
            _bworker_DataUpdator.DoWork += _bworker_DataUpdator_DoWork;
            _bworker_DataUpdator.RunWorkerAsync();
        }

       


        #region Data Classes

        // 기본 정보
        public class Info
        {
            public string KRW { get; set; }                     // 보유 원화

            public double ChangeRate_BitCoin {  get; set; }     // 비트코인 변화율

            public int N_of_RISE {  get; set; }                 // 상승 코인 수
            public int N_of_FALL { get; set; }                  // 하락 코인 수
            public int N_of_EVEN { get; set; }                  // 하락 코인 수
        }


        // 계좌 정보
        public class Accounts
        {
            public string CoinName { get; set; }        // 코인명 영문
            public string CoinNameKR { get; set; }      // 코인명 한글
            public string Volume { get; set; }          // 보유 볼륨
            public string Volume_Pending { get; set; }  // 미체결 볼륨
            public string AvgBuyPrice { get; set; }     // 매수평균가

            public bool IsExist {  get; set; }          // 보유 상태
            public decimal CurrentPrice { get; set; }   // 현재가
            public decimal Profit { get; set; }         // 수익
            public decimal ProfitRate {  get; set; }    // 수익률
        }

        // 코인 리스트
        public class CoinList
        {
            public string CoinName { get; set; }        // 코인명 영문
            public string CoinNameKR { get; set; }      // 코인명 한글
        }

        // Ticker
        public class Ticker
        {
            public string CoinName { get; set; }        // 코인명 영문
            public string CoinNameKR { get; set; }      // 코인명 한글

            public double Opening_Price {  get; set; }         // 시가
            public double High_Price { get; set; }             // 고가
            public double Low_Price { get; set; }              // 저가
            public double Trade_Price { get; set; }            // 종가
            public double Prev_Trade_Price { get; set; }       // 전일 종가

            public string Change {  get; set; }                // 보합,상승,하락
            public double Change_Price { get; set; }           // 변화액
            public double Change_Rate { get; set; }            // 변화율
            public double Trade_Volume {  get; set; }          // 거래량
        }

        // 주문 리스트
        public class Order
        {
            public string CoinName { get; set; }        // 코인명 영문
            public string CoinNameKR { get; set; }      // 코인명 한글

            public string Type {  get; set; }           // 매수, 매도
        }

        #endregion



        #region Public Methods

        // 시장가 매수
        public void Request_Buy_Market(string CoinName, string Price)
        {
            UpbitCommandParameters para = new UpbitCommandParameters();
            para.CoinName = CoinName;
            para.Order_Price = Price;

            AddCommand(UpbitCommands.시장가매수, para);
        }

        // 시장가 매도
        public void Request_Sell_Market(string CoinName)
        {
            // 계좌에 있는지 확인
            var account = _Recent_Accounts.FirstOrDefault(a => a.CoinName == CoinName);
            if (account != null)
            {
                if (account.Volume == "0")
                {
                    // 미체결 상태임
                    OrderRetrieved?.Invoke(this, new OrderEventArgs(null, "시장가 매도 실패. 미체결 상태임."));
                }
                else
                {
                    UpbitCommandParameters para = new UpbitCommandParameters();
                    para.CoinName = CoinName;
                    para.Order_Volume = account.Volume;

                    AddCommand(UpbitCommands.시장가매도, para);
                }
            }
            else
            {
                // 계좌에 없음
                OrderRetrieved?.Invoke(this, new OrderEventArgs(null, "시장가 매도 실패. 계좌에 없음."));
            }
        }




        public void Test()
        {
            
        }



        #endregion


        #region Recent Data

        private Info _Info = new Info();

        private List<CoinList> _CoinList = new List<CoinList>();

        private List<Accounts> _Recent_Accounts = new List<Accounts>();

        private List<Ticker> _Recent_Tickers = new List<Ticker>();

        #endregion




        #region Private Methods


        // 계좌 조회
        private void Request_Accounts()
        {
            AddCommand(UpbitCommands.계좌조회);
        }

        // 마켓 리스트 조회 (거래 가능 코인명 리스트)
        private void Request_MarketList()
        {
            AddCommand(UpbitCommands.마켓리스트조회);
        }

        // Ticker 조회
        private void Request_Ticker()
        {
            AddCommand(UpbitCommands.Ticker조회);
        }


        // 분 캔들 조회


        

        // 주문 리스트 조회


        // 미체결 조회


        // 호가 조회



        #endregion



        #region Data Update Background Worker


        // 정해진 시간마다 데이터 업데이트 (ticker, account 등)
        BackgroundWorker _bworker_DataUpdator = new BackgroundWorker();

        private void _bworker_DataUpdator_DoWork(object sender, DoWorkEventArgs e)
        {
            // 기본 정보 업데이트 확인 대기
            while(_Recent_Tickers.Count == 0)
            {
                Thread.Sleep(200);
            }


            // 정해진 시간마다 업데이트. (Ticker는 0.5초, 계좌는 1초)
            int cnt_account = 0;
            while(true)
            {
                cnt_account++;
                if(cnt_account == 2)
                {
                    Request_Accounts();
                    cnt_account = 0;
                }
                

                Request_Ticker();

                Thread.Sleep(500);
            }
        }


        #endregion




        #region API Methods

        // Accounts Authorization Token 생성
        private string Get_AuthorizationToken_forAccounts(Dictionary<string, string> queryParams = null)
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


        // 계좌 조회
        private async Task Get_Accounts_API()
        {
            string authorizationToken = Get_AuthorizationToken_forAccounts();

            var client = new RestClient("https://api.upbit.com/v1/accounts");
            var request = new RestRequest();
            request.Method = Method.Get;
            request.AddHeader("Authorization", authorizationToken);

            var response = await client.ExecuteAsync(request);

            List<Upbit_Account> accounts = null;
            if (response.IsSuccessful)
            {
                accounts = JsonConvert.DeserializeObject<List<Upbit_Account>>(response.Content);
            }
            else
            {
                Console.WriteLine($"API 요청 실패: {response.StatusCode} - {response.Content}");
            }

            // 계좌 있는지 확인
            if (accounts != null && accounts.Count > 0)
            {
                List<Accounts> tempAccounts = new List<Accounts>();

                // 기본 내용 옮기기
                tempAccounts = accounts
                    .Select(s => new Accounts {
                        CoinName = s.Currency,
                        CoinNameKR = s.Currency != "KRW" ? _CoinList.FirstOrDefault(c => c.CoinName == s.Currency).CoinNameKR : "",
                        Volume = s.Balance, 
                        Volume_Pending = s.Locked, 
                        AvgBuyPrice = s.AvgBuyPrice })
                    .ToList();

                // KRW는 제거
                tempAccounts.RemoveAll(a => a.CoinName == "KRW");

                // 나머지 내용 체우기
                _Info.KRW = accounts.FirstOrDefault(b => b.Currency == "KRW").Balance;


                // 최근 정보에 저장
                _Recent_Accounts = tempAccounts.ToList();


                // Deep Copy
                List<Accounts> ret = tempAccounts.Select(a => new Accounts 
                {
                    CoinName = a.CoinName,
                    CoinNameKR = a.CoinNameKR,
                    Volume = a.Volume,
                    Volume_Pending = a.Volume_Pending,
                    AvgBuyPrice = a.AvgBuyPrice,
                    IsExist = a.IsExist,
                    CurrentPrice = a.CurrentPrice,
                    Profit = a.Profit,
                    ProfitRate = a.ProfitRate
                }
                ).ToList();

                // 성공 시 이벤트 발생
                AccountsRetrieved?.Invoke(this, new AccountEventArgs(ret, "계좌 정보를 성공적으로 가져왔습니다."));
            }
            else
            {
                // 실패 시 이벤트 발생
                AccountsRetrieved?.Invoke(this, new AccountEventArgs(null, "계좌 정보를 가져오는 데 실패했습니다."));
            }
        }


        // 코인(마켓)리스트 가져오기
        private async Task Get_CoinList_API()
        {
            // Query Parameter
            var queryParams = new Dictionary<string, string>
            {
                { "is_details", "true" }
            };

            string authorizationToken = Get_AuthorizationToken_forAccounts(queryParams);

            var client = new RestClient("https://api.upbit.com/v1/market/all");
            var request = new RestRequest();
            request.Method = Method.Get;
            request.AddHeader("Authorization", authorizationToken);

            var response = await client.ExecuteAsync(request);

            if (response.IsSuccessful)
            {
                // JSON 데이터를 List<Upbit_Market> 형식으로 역직렬화
                List<Upbit_Market> allCoins = JsonConvert.DeserializeObject<List<Upbit_Market>>(response.Content);

                // 코인 리스트에 저장
                _CoinList = allCoins
                    .Where(coin => coin.Name_Market.StartsWith("KRW-")) // KRW-로 시작하는 항목만 필터링
                    .Select(coin => new CoinList
                    {
                        CoinName = coin.Name_Market.Replace("KRW-", ""), // KRW- 제거
                        CoinNameKR = coin.Name_KR,
                    })
                    .OrderBy(coin => coin.CoinNameKR) // Name_KR 기준으로 정렬
                    .ToList();
            }
            else
            {
                Console.WriteLine($"API 요청 실패: {response.StatusCode} - {response.Content}");
            }
        }


        // Ticker 조회
        private async Task Get_Ticker_API()
        {
            string authorizationToken = Get_AuthorizationToken_forAccounts();

            var client = new RestClient("https://api.upbit.com/v1/ticker/all?quote_currencies=KRW");
            var request = new RestRequest();
            request.Method = Method.Get;
            request.AddHeader("Authorization", authorizationToken);

            var response = await client.ExecuteAsync(request);

            //Console.WriteLine(response.Content);

            if (response.IsSuccessful)
            {
                // JSON 데이터를 List<TickerData> 형식으로 역직렬화
                var allTickers = JsonConvert.DeserializeObject<List<Upbit_Ticker>>(response.Content);

                // KRW-로 시작하는 코인만 필터링
                List<Ticker> filteredAllTickers = allTickers
                    .Where(ticker => ticker.Market.StartsWith("KRW-")) // KRW-로 시작하는 항목만 필터링
                    .Select(ticker => new Ticker
                    {
                        CoinName = ticker.Market.Replace("KRW-", ""), // KRW- 제거
                        CoinNameKR = _CoinList.FirstOrDefault(c => c.CoinName == ticker.Market.Replace("KRW-", "")).CoinNameKR,
                       
                        Opening_Price = ticker.OpeningPrice,
                        High_Price = ticker.HighPrice,
                        Low_Price = ticker.LowPrice,
                        Trade_Price = ticker.TradePrice,
                        Prev_Trade_Price = ticker.PrevClosingPrice,

                        Change = ticker.Change == "FALL" ? "하락" : ticker.Change == "RISE" ? "상승" : "보합",
                        Change_Price = ticker.SignedChangePrice,
                        Change_Rate = ticker.SignedChangeRate,
                        Trade_Volume = ticker.TradeVolume,
                    })
                    .OrderBy(ticker => ticker.CoinNameKR) // Market 기준으로 정렬
                    .ToList();

                // Info 불러와서 업데이트
                _Info.N_of_RISE = filteredAllTickers.FindAll(rise => rise.Change == "상승").Count;
                _Info.N_of_EVEN = filteredAllTickers.FindAll(rise => rise.Change == "보합").Count;
                _Info.N_of_FALL = filteredAllTickers.FindAll(rise => rise.Change == "하락").Count;

                _Info.ChangeRate_BitCoin = filteredAllTickers.FirstOrDefault(b => b.CoinNameKR == "비트코인").Change_Rate;

                // 최근 ticker 업데이트
                _Recent_Tickers = filteredAllTickers.ToList();

                // Deep Copy
                List<Ticker> ret = filteredAllTickers.Select(a => new Ticker
                {
                    CoinName = a.CoinName,
                    CoinNameKR = a.CoinNameKR,
                    Opening_Price = a.Opening_Price,
                    High_Price = a.High_Price,
                    Low_Price = a.Low_Price,
                    Trade_Price = a.Trade_Price,
                    Prev_Trade_Price = a.Prev_Trade_Price,
                    Change = a.Change,
                    Change_Rate = a.Change_Rate,
                    Change_Price = a.Change_Price,
                    Trade_Volume = a.Trade_Volume
                }
                ).ToList();

                // 성공 시 이벤트 발생
                TickersRetrieved?.Invoke(this, new TickerEventArgs(ret, "Ticker 성공."));
            }
            else
            {
                // 실패 시 이벤트 발생
                TickersRetrieved?.Invoke(this, new TickerEventArgs(null, "Ticker 실패."));
                Console.WriteLine($"API 요청 실패: {response.StatusCode} - {response.Content}");
            }

        }

        // 분 캔들 조회

        // 시장가 매수 요청
        private async Task Order_Buy_Market_API(string CoinName, string Price)
        {
            // 1. Query 문자열 생성
            var query = $"market=KRW-{CoinName}&side=bid&volume=&price={Price}&ord_type=price";

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
                new System.Security.Claims.Claim("access_key", _accessKey),
                new System.Security.Claims.Claim("nonce", Guid.NewGuid().ToString()),
                new System.Security.Claims.Claim("query_hash", queryHash),
                new System.Security.Claims.Claim("query_hash_alg", "SHA512")
            };

            var securityKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));
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
                market = "KRW-" + CoinName,
                side = "bid",
                volume = "",
                price = Price,
                ord_type = "price"
            };
            request.AddJsonBody(body);

            // 6. API 요청 및 응답 처리
            var response = await client.ExecuteAsync(request);
            if (response.IsSuccessful)
            {
                Console.WriteLine("주문 요청 성공!");
                Order order = new Order();

                order.CoinName = CoinName;
                order.CoinNameKR = _CoinList.FirstOrDefault(c => c.CoinName == CoinName).CoinNameKR;
                order.Type = "시장가매수";

                // 성공 시 이벤트 발생
                OrderRetrieved?.Invoke(this, new OrderEventArgs(order, "주문 성공"));
            }
            else
            {
                Console.WriteLine($"주문 요청 실패: {response.StatusCode} - {response.Content}");
                // 실패 시 이벤트 발생
                OrderRetrieved?.Invoke(this, new OrderEventArgs(null, "주문 실패"));
            }
        }

        // 시장가 매수 요청
        private async Task Order_Sell_Market_API(string CoinName, string Volume)
        {
            // 1. Query 문자열 생성
            var query = $"market=KRW-{CoinName}&side=ask&volume={Volume}&price=&ord_type=market";

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
                new System.Security.Claims.Claim("access_key", _accessKey),
                new System.Security.Claims.Claim("nonce", Guid.NewGuid().ToString()),
                new System.Security.Claims.Claim("query_hash", queryHash),
                new System.Security.Claims.Claim("query_hash_alg", "SHA512")
            };

            var securityKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));
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
                market = "KRW-" + CoinName,
                side = "ask",
                volume = Volume,
                price = "",
                ord_type = "market"
            };
            request.AddJsonBody(body);

            // 6. API 요청 및 응답 처리
            var response = await client.ExecuteAsync(request);
            if (response.IsSuccessful)
            {
                Console.WriteLine("주문 요청 성공!");
                Order order = new Order();

                order.CoinName = CoinName;
                order.CoinNameKR = _CoinList.FirstOrDefault(c => c.CoinName == CoinName).CoinNameKR;
                order.Type = "시장가매도";

                // 성공 시 이벤트 발생
                OrderRetrieved?.Invoke(this, new OrderEventArgs(order, "주문 성공"));
            }
            else
            {
                Console.WriteLine($"주문 요청 실패: {response.StatusCode} - {response.Content}");
                // 실패 시 이벤트 발생
                OrderRetrieved?.Invoke(this, new OrderEventArgs(null, "주문 실패"));
            }
        }

        // 주문 리스트 조회

        // 미체결 조회

        // 호가 조회

        #endregion





        #region 서버 요청 처리 및 제한 핸들러


        private readonly object _cmdLock = new object();
        private readonly List<Upbit_Command> _CMD_Requests = new List<Upbit_Command>();

        // 명령 포맷
        private class Upbit_Command
        {
            public UpbitCommands Command { get; set; }
            public UpbitCommandParameters Parameters { get; set; }
        }

        // 명령어
        private enum UpbitCommands
        {
            계좌조회 = 0,
            마켓리스트조회 = 1,
            Ticker조회 = 2,
            분캔들조회 = 3,
            시장가매수 = 4,
            시장가매도 = 5,
            주문리스트조회 = 6,
            미체결조회 = 7,
            호가조회 = 8
        }

        // 파라메터
        private class UpbitCommandParameters
        {
            public string CoinName { get; set; }
            public string Order_Price {  get; set; }        
            public string Order_Volume {  get; set; }
        }



        // 명령 넣기
        private void AddCommand(UpbitCommands command, UpbitCommandParameters parameters = null)
        {
            lock (_cmdLock)
            {
                _CMD_Requests.Add(new Upbit_Command
                {
                    Command = command,
                    Parameters = parameters
                });
            }
        }


        // 서버 요청 처리 태스크
        private async Task ProcessCommandsAsync()
        {
            while (true)
            {
                var command = GetNextCommand();
                if (command != null)
                {
                    switch (command.Command)
                    {
                        case UpbitCommands.계좌조회:
                            await _RequestCounter_Order.ProcessRequestAsync(async () =>
                            {
                                await Get_Accounts_API();
                            });
                            break;
                        case UpbitCommands.마켓리스트조회:
                            await _RequestCounter_Order.ProcessRequestAsync(async () =>
                            {
                                await Get_CoinList_API();
                            });
                            break;
                        case UpbitCommands.Ticker조회:
                            await _RequestCounter_Order.ProcessRequestAsync(async () =>
                            {
                                await Get_Ticker_API();
                            });
                            break;
                        case UpbitCommands.시장가매수:
                            await _RequestCounter_Order.ProcessRequestAsync(async () =>
                            {
                                await Order_Buy_Market_API(command.Parameters.CoinName, command.Parameters.Order_Price);
                            });
                            break;
                        case UpbitCommands.시장가매도:
                            await _RequestCounter_Order.ProcessRequestAsync(async () =>
                            {
                                await Order_Sell_Market_API(command.Parameters.CoinName, command.Parameters.Order_Volume);
                            });
                            break;
                    }
                }
                else
                {
                    await Task.Delay(100); // 명령이 없으면 대기
                }
            }
        }

        private Upbit_Command GetNextCommand()
        {
            lock (_cmdLock)
            {
                if (_CMD_Requests.Count > 0)
                {
                    var command = _CMD_Requests[0];
                    _CMD_Requests.RemoveAt(0);
                    return command;
                }
                return null;
            }
        }

        // 서버 요청 제한 핸들러
        private readonly ApiRequestHandler _RequestCounter_Order = new ApiRequestHandler(8, TimeSpan.FromSeconds(1));
        class ApiRequestHandler
        {
            private readonly int _maxRequestsPerInterval;
            private readonly TimeSpan _interval;
            private readonly SemaphoreSlim _semaphore;
            private readonly Queue<DateTime> _requestTimestamps;

            public ApiRequestHandler(int maxRequestsPerInterval, TimeSpan interval)
            {
                _maxRequestsPerInterval = maxRequestsPerInterval;
                _interval = interval;
                _semaphore = new SemaphoreSlim(maxRequestsPerInterval, maxRequestsPerInterval);
                _requestTimestamps = new Queue<DateTime>();
            }

            public async Task<bool> ProcessRequestAsync(Func<Task> apiRequest)
            {
                lock (_requestTimestamps)
                {
                    // 오래된 요청 시간 제거
                    while (_requestTimestamps.Count > 0 &&
                           (DateTime.UtcNow - _requestTimestamps.Peek()) > _interval)
                    {
                        _requestTimestamps.Dequeue();
                        _semaphore.Release();
                    }
                }

                // 요청 가능 여부 확인
                if (await _semaphore.WaitAsync(TimeSpan.FromSeconds(1)))
                {
                    try
                    {
                        lock (_requestTimestamps)
                        {
                            _requestTimestamps.Enqueue(DateTime.UtcNow);
                        }

                        // API 요청 실행
                        await apiRequest();
                        return true;
                    }
                    finally
                    {
                        // 요청 간 대기
                    }
                }

                return false;
            }
        }

        #endregion




        #region UpbitAPI Response Classes

        // 계좌 조회
        private class Upbit_Account
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


        // 코인(마켓) 리스트 (거래 가능 코인명 리스트)
        private class Upbit_Market
        {
            [JsonProperty("market")]
            public string Name_Market { get; set; }

            [JsonProperty("korean_name")]
            public string Name_KR { get; set; }

            [JsonProperty("english_name")]
            public string Name_ENG { get; set; }
        }

        // Ticker 조회
        private class Upbit_Ticker
        {
            [JsonProperty("market")]
            public string Market { get; set; } // 종목 구분 코드

            [JsonProperty("trade_date")]
            public string TradeDateUTC { get; set; } // 최근 거래 일자 (UTC)

            [JsonProperty("trade_time")]
            public string TradeTimeUTC { get; set; } // 최근 거래 시간 (UTC)

            [JsonProperty("trade_date_kst")]
            public string TradeDateKST { get; set; } // 최근 거래 일자 (KST)

            [JsonProperty("trade_time_kst")]
            public string TradeTimeKST { get; set; } // 최근 거래 시간 (KST)

            [JsonProperty("trade_timestamp")]
            public long TradeTimestamp { get; set; } // 최근 거래 일시 (UTC, Unix Timestamp)

            [JsonProperty("opening_price")]
            public double OpeningPrice { get; set; } // 시가

            [JsonProperty("high_price")]
            public double HighPrice { get; set; } // 고가

            [JsonProperty("low_price")]
            public double LowPrice { get; set; } // 저가

            [JsonProperty("trade_price")]
            public double TradePrice { get; set; } // 현재가

            [JsonProperty("prev_closing_price")]
            public double PrevClosingPrice { get; set; } // 전일 종가

            [JsonProperty("change")]
            public string Change { get; set; } // 변동 상태 (EVEN, RISE, FALL)

            [JsonProperty("change_price")]
            public double ChangePrice { get; set; } // 변동액의 절대값

            [JsonProperty("change_rate")]
            public double ChangeRate { get; set; } // 변동률의 절대값

            [JsonProperty("signed_change_price")]
            public double SignedChangePrice { get; set; } // 부호가 있는 변동액

            [JsonProperty("signed_change_rate")]
            public double SignedChangeRate { get; set; } // 부호가 있는 변동률

            [JsonProperty("trade_volume")]
            public double TradeVolume { get; set; } // 가장 최근 거래량

            [JsonProperty("acc_trade_price")]
            public double AccTradePrice { get; set; } // 누적 거래대금 (UTC 0시 기준)

            [JsonProperty("acc_trade_price_24h")]
            public double AccTradePrice24h { get; set; } // 24시간 누적 거래대금

            [JsonProperty("acc_trade_volume")]
            public double AccTradeVolume { get; set; } // 누적 거래량 (UTC 0시 기준)

            [JsonProperty("acc_trade_volume_24h")]
            public double AccTradeVolume24h { get; set; } // 24시간 누적 거래량

            [JsonProperty("highest_52_week_price")]
            public double Highest52WeekPrice { get; set; } // 52주 신고가

            [JsonProperty("highest_52_week_date")]
            public string Highest52WeekDate { get; set; } // 52주 신고가 달성일

            [JsonProperty("lowest_52_week_price")]
            public double Lowest52WeekPrice { get; set; } // 52주 신저가

            [JsonProperty("lowest_52_week_date")]
            public string Lowest52WeekDate { get; set; } // 52주 신저가 달성일

            [JsonProperty("timestamp")]
            public long Timestamp { get; set; } // 타임스탬프
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


        // 주문 요청
        public class Upbit_Order
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


        // 주문 리스트 조회
        public class Upbit_OrderList
        {
            [JsonProperty("uuid")]
            public string Uuid { get; set; } // 주문 고유 아이디

            [JsonProperty("side")]
            public string Side { get; set; } // 주문 종류 (bid: 매수, ask: 매도)

            [JsonProperty("ord_type")]
            public string OrdType { get; set; } // 주문 방식 (limit, price, market, best)

            [JsonProperty("price")]
            public string Price { get; set; } // 주문 당시 화폐 가격 (NumberString)

            [JsonProperty("avg_price")]
            public string AvgPrice { get; set; } // 주문 당시 화폐 가격 (NumberString)

            [JsonProperty("state")]
            public string State { get; set; } // 주문 상태 (done: 체결 완료, cancel: 취소됨)

            [JsonProperty("market")]
            public string Market { get; set; } // 마켓 ID (예: KRW-BTC, KRW-ETH)

            [JsonProperty("created_at")]
            public string CreatedAt { get; set; } // 주문 생성 시각 (DateString)

            [JsonProperty("volume")]
            public string Volume { get; set; } // 사용자가 입력한 주문량 (NumberString)

            [JsonProperty("remaining_volume")]
            public string RemainingVolume { get; set; } // 체결 후 남은 주문량 (NumberString)

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
            public int TradesCount { get; set; } // 해당 주문에 걸린 체결 수 (Integer)

            [JsonProperty("time_in_force")]
            public string TimeInForce { get; set; } // IOC / FOK 설정 값 (String)
        }


        // 미체결 조회
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


        // 호가 조회
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


        // 호가 유닛
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




        #endregion






        #region Events

        // 계좌 업데이트 이벤트
        public event EventHandler<AccountEventArgs> AccountsRetrieved;
        public class AccountEventArgs : EventArgs
        {
            public List<Accounts> Accounts { get; }
            public string Message { get; }

            public AccountEventArgs(List<Accounts> accounts, string message)
            {
                Accounts = accounts;
                Message = message;
            }
        }

        // Ticker 업데이트 이벤트
        public event EventHandler<TickerEventArgs> TickersRetrieved;
        public class TickerEventArgs : EventArgs
        {
            public List<Ticker> Tickers { get; }
            public string Message { get; }

            public TickerEventArgs(List<Ticker> tickers, string message)
            {
                Tickers = tickers;
                Message = message;
            }
        }

        // Ticker 업데이트 이벤트
        public event EventHandler<OrderEventArgs> OrderRetrieved;
        public class OrderEventArgs : EventArgs
        {
            public Order Order { get; }
            public string Message { get; }

            public OrderEventArgs(Order order, string message)
            {
                Order = order;
                Message = message;
            }
        }

        #endregion





    }
}
