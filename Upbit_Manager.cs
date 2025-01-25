using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using static GGo_v1.UpbitAPI.Upbit_Account;
using System.Web.UI.WebControls;
using static GGo_v1.UpbitAPI.Upbit_Candle;
using static GGo_v1.UpbitAPI.Upbit_Order;

namespace GGo_v1.UpbitAPI
{
    public class Upbit_Manager
    {
        private Upbit_Account _Upbit_Account;       // 계좌 관리 클래스
        private Upbit_Candle _Upbit_Candle;         // 캔들 관리 클래스
        private Upbit_Order _Upbit_Order;         // 주문 관리 클래스
        private Upbit_Orderbook _Upbit_Orderbook;   // 호가 관리 클래스

        private readonly object _cmdLock = new object();
        private readonly List<CommandData> _CMD_Requests = new List<CommandData>();

        // 요청 제한 핸들러
        private readonly ApiRequestHandler _RequestCounter_Normal = new ApiRequestHandler(30, TimeSpan.FromSeconds(1));
        private readonly ApiRequestHandler _RequestCounter_Order = new ApiRequestHandler(8, TimeSpan.FromSeconds(1));
        //private readonly ApiRequestHandler _RequestCounter_Cancel_Order = new ApiRequestHandler(1, TimeSpan.FromSeconds(2));

        // 이벤트 선언
        public event EventHandler<AccountEventArgs> AccountsRetrieved;
        public event EventHandler<CoinListEventArgs> CoinListRetrieved;
        public event EventHandler<TickerEventArgs> TickerRetrieved;
        public event EventHandler<CandleEventArgs> CandleRetrieved;
        public event EventHandler<OrderEventArgs> OrderRetrieved;
        public event EventHandler<PendingOrderEventArgs> PendingOrderRetrieved;
        public event EventHandler<OrderbookEventArgs> OrderbookRetrieved;

        public Upbit_Manager()
        {
#if DEBUG
            // 내꺼 (debug 모드)
            _Upbit_Account = new Upbit_Account("mTnFWBLWmmMVpPja5LxWNMNbp3j7tdz9la269qlk", "zvJ8DV0wWBTOaxp8dr4dzIb18xJRmqRyU5Q06Aro");
            _Upbit_Candle = new Upbit_Candle("mTnFWBLWmmMVpPja5LxWNMNbp3j7tdz9la269qlk", "zvJ8DV0wWBTOaxp8dr4dzIb18xJRmqRyU5Q06Aro");
            _Upbit_Order = new Upbit_Order("mTnFWBLWmmMVpPja5LxWNMNbp3j7tdz9la269qlk", "zvJ8DV0wWBTOaxp8dr4dzIb18xJRmqRyU5Q06Aro");
            _Upbit_Orderbook = new Upbit_Orderbook("mTnFWBLWmmMVpPja5LxWNMNbp3j7tdz9la269qlk", "zvJ8DV0wWBTOaxp8dr4dzIb18xJRmqRyU5Q06Aro");
#else
            // 땡수엉꺼 (release 모드)
            _Upbit_Account = new Upbit_Account("UKkDoiThWqAOvReatRsDgveuzEo2Jqklvmb0YRPX", "6wz5a1F5osQeBxQeZlwwpovhx33YnzxqJ9PRFHDr");
            _Upbit_Candle = new Upbit_Candle("UKkDoiThWqAOvReatRsDgveuzEo2Jqklvmb0YRPX", "6wz5a1F5osQeBxQeZlwwpovhx33YnzxqJ9PRFHDr");
            _Upbit_Order = new Upbit_Order("UKkDoiThWqAOvReatRsDgveuzEo2Jqklvmb0YRPX", "6wz5a1F5osQeBxQeZlwwpovhx33YnzxqJ9PRFHDr");
            _Upbit_Orderbook = new Upbit_Orderbook("UKkDoiThWqAOvReatRsDgveuzEo2Jqklvmb0YRPX", "6wz5a1F5osQeBxQeZlwwpovhx33YnzxqJ9PRFHDr");
#endif

            // 명령 처리 Task 실행
            Task.Run(ProcessCommandsAsync);
        }





        #region 명령 처리 태스크
        private async Task ProcessCommandsAsync()
        {
            while (true)
            {
                var command = GetNextCommand();
                if (command != null)
                {
                    switch (command.Command)
                    {
                        case "시장가매도주문요청":
                            await _RequestCounter_Order.ProcessRequestAsync(async () =>
                            {
                                await Sell_Order_Market(command);
                            });
                            break;

                        case "시장가매수주문요청":
                            await _RequestCounter_Order.ProcessRequestAsync(async () =>
                            {
                                await Buy_Order_Market(command);
                            });
                            break;

                        case "지정가매도주문요청":
                            await _RequestCounter_Order.ProcessRequestAsync(async () =>
                            {
                                await Sell_Order_Limit(command);
                            });
                            break;

                        case "지정가매수주문요청":
                            await _RequestCounter_Order.ProcessRequestAsync(async () =>
                            {
                                await Buy_Order_Limit(command);
                            });
                            break;

                        case "미체결내역조회요청":
                            await _RequestCounter_Order.ProcessRequestAsync(async () =>
                            {
                                await Get_PendingOrders();
                            });
                            break;


                        case "미체결취소요청":
                            await _RequestCounter_Order.ProcessRequestAsync(async () =>
                            {
                                await Cancel_PendingOrder(command);
                            });
                            break;


                        case "호가요청":
                            await _RequestCounter_Order.ProcessRequestAsync(async () =>
                            {
                                await Get_Orderbook(command);
                            });
                            break;




                        case "계좌정보읽기요청":
                            await _RequestCounter_Normal.ProcessRequestAsync(async () =>
                            {
                                await ReadAccount_Process();
                            });
                            break;

                        case "전체코인리스트요청":
                            await _RequestCounter_Normal.ProcessRequestAsync(async () =>
                            {
                                await ReadCoinList_Process();
                            });
                            break;

                        case "전체코인시세요청":
                            await _RequestCounter_Normal.ProcessRequestAsync(async () =>
                            {
                                await ReadTicker_Process();
                            });
                            break;

                        case "분캔들요청":
                            await _RequestCounter_Normal.ProcessRequestAsync(async () =>
                            {
                                await ReadCandle_Process(command);
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

        private void AddCommand(string command, Dictionary<string, object> parameters = null)
        {
            lock (_cmdLock)
            {
                _CMD_Requests.Add(new CommandData
                {
                    Command = command,
                    Parameters = parameters
                });
            }
        }

        private CommandData GetNextCommand()
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

        public class CommandData
        {
            public string Command { get; set; }
            public Dictionary<string, object> Parameters { get; set; }
        }


        #endregion



        #region API 호출

        // 시장가 매도 주문
        private async Task Sell_Order_Market(CommandData commandData)
        {
            Upbit_Order.Order_Response order_response = await _Upbit_Order.Sell_Order_MarketPrice((string)commandData.Parameters["coinName"], (string)commandData.Parameters["volume"]);
            if (order_response != null)
            {
                // 성공 시 이벤트 발생
                OrderRetrieved?.Invoke(this, new OrderEventArgs(order_response, "주문 결과를 성공적으로 가져왔습니다."));
            }
            else
            {
                // 실패 시 이벤트 발생
                OrderRetrieved?.Invoke(this, new OrderEventArgs(null, "주문 결과를 가져오는 데 실패했습니다."));
            }
        }

        // 시장가 매수 주문
        private async Task Buy_Order_Market(CommandData commandData)
        {
            Upbit_Order.Order_Response order_response = await _Upbit_Order.Buy_Order_MarketPrice((string)commandData.Parameters["coinName"], (string)commandData.Parameters["price"]);
            if (order_response != null)
            {
                // 성공 시 이벤트 발생
                OrderRetrieved?.Invoke(this, new OrderEventArgs(order_response, "주문 결과를 성공적으로 가져왔습니다."));
            }
            else
            {
                // 실패 시 이벤트 발생
                OrderRetrieved?.Invoke(this, new OrderEventArgs(null, "주문 결과를 가져오는 데 실패했습니다."));
            }
        }

        // 지정가 매도 주문
        private async Task Sell_Order_Limit(CommandData commandData)
        {
            Upbit_Order.Order_Response order_response = await _Upbit_Order.Sell_Order_LimitPrice((string)commandData.Parameters["coinName"], (string)commandData.Parameters["volume"], (string)commandData.Parameters["price"]);
            if (order_response != null)
            {
                // 성공 시 이벤트 발생
                OrderRetrieved?.Invoke(this, new OrderEventArgs(order_response, "주문 결과를 성공적으로 가져왔습니다."));
            }
            else
            {
                // 실패 시 이벤트 발생
                OrderRetrieved?.Invoke(this, new OrderEventArgs(null, "주문 결과를 가져오는 데 실패했습니다."));
            }
        }

        // 지정가 매수 주문
        private async Task Buy_Order_Limit(CommandData commandData)
        {
            Upbit_Order.Order_Response order_response = await _Upbit_Order.Buy_Order_LimitPrice((string)commandData.Parameters["coinName"], (string)commandData.Parameters["volume"], (string)commandData.Parameters["price"]);
            if (order_response != null)
            {
                // 성공 시 이벤트 발생
                OrderRetrieved?.Invoke(this, new OrderEventArgs(order_response, "주문 결과를 성공적으로 가져왔습니다."));
            }
            else
            {
                // 실패 시 이벤트 발생
                OrderRetrieved?.Invoke(this, new OrderEventArgs(null, "주문 결과를 가져오는 데 실패했습니다."));
            }
        }


        // 미체결 조회
        private async Task Get_PendingOrders()
        {
            List<Upbit_Order.PendingOrder> pendingOrders = await _Upbit_Order.Get_PendingOrders();
            if (pendingOrders != null && pendingOrders.Count > 0)
            {
                // 성공 시 이벤트 발생
                PendingOrderRetrieved?.Invoke(this, new PendingOrderEventArgs(pendingOrders, "미체결 정보를 성공적으로 가져왔습니다."));
            }
            else
            {
                // 실패 시 이벤트 발생
                PendingOrderRetrieved?.Invoke(this, new PendingOrderEventArgs(null, "미체결 정보를 가져오는 데 실패했습니다."));
            }
        }

        // 미체결 주문 취소
        private async Task Cancel_PendingOrder(CommandData commandData)
        {
            string uuid = (string)commandData.Parameters["uuid"];

            Upbit_Order.PendingOrder temppendingOrder = await _Upbit_Order.Delete_PendingOrder(uuid);
            List<Upbit_Order.PendingOrder> List_pendingOrders = new List<PendingOrder>();
            List_pendingOrders.Add(temppendingOrder);
            if (temppendingOrder != null)
            {
                // 성공 시 이벤트 발생
                PendingOrderRetrieved?.Invoke(this, new PendingOrderEventArgs(List_pendingOrders, "미체결 정보를 성공적으로 가져왔습니다."));
            }
            else
            {
                // 실패 시 이벤트 발생
                PendingOrderRetrieved?.Invoke(this, new PendingOrderEventArgs(null, "미체결 정보를 가져오는 데 실패했습니다."));
            }
        }


        // 호가 요청
        private async Task Get_Orderbook(CommandData commandData)
        {
            Upbit_Orderbook.Orderbook orderbooks = await _Upbit_Orderbook.Get_Orderbook((string)commandData.Parameters["market"]);
            if (orderbooks != null)
            {
                // 성공 시 이벤트 발생
                OrderbookRetrieved?.Invoke(this, new OrderbookEventArgs(orderbooks, "미체결 정보를 성공적으로 가져왔습니다."));
            }
            else
            {
                // 실패 시 이벤트 발생
                OrderbookRetrieved?.Invoke(this, new OrderbookEventArgs(null, "미체결 정보를 가져오는 데 실패했습니다."));
            }

        }



        // 계좌 읽기
        private async Task ReadAccount_Process()
        {
            List<Upbit_Account.Account> accounts = await _Upbit_Account.Get_Accounts();
            if (accounts != null && accounts.Count > 0)
            {
                // 성공 시 이벤트 발생
                AccountsRetrieved?.Invoke(this, new AccountEventArgs(accounts, "계좌 정보를 성공적으로 가져왔습니다."));
            }
            else
            {
                // 실패 시 이벤트 발생
                AccountsRetrieved?.Invoke(this, new AccountEventArgs(null, "계좌 정보를 가져오는 데 실패했습니다."));
            }
        }

        // 코인 리스트 읽기
        private async Task ReadCoinList_Process()
        {
            List<Upbit_Account.CoinName> coinNames = await _Upbit_Account.Get_CoinList();
            if (coinNames != null && coinNames.Count > 0)
            {
                // 성공 시 이벤트 발생
                CoinListRetrieved?.Invoke(this, new CoinListEventArgs(coinNames, "코인 리스트를 성공적으로 가져왔습니다."));
            }
            else
            {
                // 실패 시 이벤트 발생
                CoinListRetrieved?.Invoke(this, new CoinListEventArgs(null, "코인 리스트를 가져오는 데 실패했습니다."));
            }
        }

        // 전체 코인 시세 읽기
        private async Task ReadTicker_Process()
        {
            List<Upbit_Account.TickerData> tickers = await _Upbit_Account.Get_All_Tickers();
            if (tickers != null && tickers.Count > 0)
            {
                // 성공 시 이벤트 발생
                TickerRetrieved?.Invoke(this, new TickerEventArgs(tickers, "코인 시세를 성공적으로 가져왔습니다."));
            }
            else
            {
                // 실패 시 이벤트 발생
                TickerRetrieved?.Invoke(this, new TickerEventArgs(null, "코인 시세를 가져오는 데 실패했습니다."));
            }
        }


        // 분 캔들 읽기
        private async Task ReadCandle_Process(CommandData commandData)
        {
            List<Upbit_Candle.Candle> candles = await _Upbit_Candle.Get_Minute_Candles((string)commandData.Parameters["coinName"], (int)commandData.Parameters["unit"], (int)commandData.Parameters["ma_period"]);
            if (candles != null && candles.Count > 0)
            {
                // 성공 시 이벤트 발생
                CandleRetrieved?.Invoke(this, new CandleEventArgs(candles, "분 캔들 정보를 성공적으로 가져왔습니다."));
            }
            else
            {
                // 실패 시 이벤트 발생
                CandleRetrieved?.Invoke(this, new CandleEventArgs(null, "분 캔들 정보를 가져오는 데 실패했습니다."));
            }
        }


        #endregion



        #region 명령 요청

        // 시장가 매도
        public void Request_SellOrder_Market(string coinName, string volume)
        {
            var parameters = new Dictionary<string, object>
            {
                { "coinName", coinName },
                { "volume", volume }
            };

            AddCommand("시장가매도주문요청", parameters);
        }

        // 시장가 매수
        public void Request_BuyOrder_Market(string coinName, string price)
        {
            var parameters = new Dictionary<string, object>
            {
                { "coinName", coinName },
                { "price", price }
            };

            AddCommand("시장가매수주문요청", parameters);
        }

        // 지정가 매도
        public void Request_SellOrder_Limit(string coinName, string TargetPrice, string volume)
        {

            var parameters = new Dictionary<string, object>
            {
                { "coinName", coinName },
                { "volume", volume },
                { "price", TargetPrice }
            };

            AddCommand("지정가매도주문요청", parameters);
        }


        // 지정가 매수
        public void Request_BuyOrder_Limit(string coinName, string TargetPrice, string TotalBuyingPrice)
        {
            // volume 게산
            decimal volume = decimal.Parse(TotalBuyingPrice) / decimal.Parse(TargetPrice);

            var parameters = new Dictionary<string, object>
            {
                { "coinName", coinName },
                { "volume", volume.ToString() },
                { "price", TargetPrice }
            };

            AddCommand("지정가매수주문요청", parameters);
        }


        // 미체결 내역 조회
        public void Request_PendingOrders()
        {
            AddCommand("미체결내역조회요청");
        }

        // 미체결 취소
        public void Request_CancelPendingOrders(string uuid)
        {
            var parameters = new Dictionary<string, object>
            {
                { "uuid", uuid }
            };

            AddCommand("미체결취소요청", parameters);
        }

        // 호가 요청
        public void Request_Orderbook(string market)
        {
            var parameters = new Dictionary<string, object>
            {
                { "market", market }
            };

            AddCommand("호가요청", parameters);
        }



        public void Request_Accounts_Info()
        {
            AddCommand("계좌정보읽기요청");
        }

        public void Request_CoinList()
        {
            AddCommand("전체코인리스트요청");
        }

        public void Request_Tickers()
        {
            AddCommand("전체코인시세요청");
        }

        public void Request_Candles(string coinName, int unit, int MA_Period)
        {
            var parameters = new Dictionary<string, object>
            {
                { "coinName", coinName },
                { "unit", unit },
                { "ma_period", MA_Period }
            };

            AddCommand("분캔들요청", parameters);
        }


        #endregion



        #region ApiRequestHandler
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
    }



    #region 이벤트 관련 클래스

    // 계좌 정보 이벤트
    public class AccountEventArgs : EventArgs
    {
        public List<Upbit_Account.Account> Accounts { get; }
        public string Message { get; }

        public AccountEventArgs(List<Upbit_Account.Account> accounts, string message)
        {
            Accounts = accounts;
            Message = message;
        }
    }

    // 코인 리스트 이벤트
    public class CoinListEventArgs : EventArgs
    {
        public List<Upbit_Account.CoinName> CoinLists { get; }
        public string Message { get; }

        public CoinListEventArgs(List<Upbit_Account.CoinName> coinLists, string message)
        {
            CoinLists = coinLists;
            Message = message;
        }
    }


    // 코인 시세 이벤트
    public class TickerEventArgs : EventArgs
    {
        public List<Upbit_Account.TickerData> Tickers { get; }
        public string Message { get; }

        public TickerEventArgs(List<Upbit_Account.TickerData> tickers, string message)
        {
            Tickers = tickers;
            Message = message;
        }
    }



    // 분 캔들 정보 이벤트
    public class CandleEventArgs : EventArgs
    {
        public List<Upbit_Candle.Candle> Candles { get; }
        public string Message { get; }

        public CandleEventArgs(List<Upbit_Candle.Candle> candles, string message)
        {
            Candles = candles;
            Message = message;
        }
    }


    // 주문 요청 이벤트
    public class OrderEventArgs : EventArgs
    {
        public Upbit_Order.Order_Response order_response { get; }
        public string Message { get; }

        public OrderEventArgs(Upbit_Order.Order_Response candles, string message)
        {
            order_response = candles;
            Message = message;
        }
    }

    // 미체결 조회 이벤트
    public class PendingOrderEventArgs : EventArgs
    {
        public List<Upbit_Order.PendingOrder> pendingOrders { get; }
        public string Message { get; }

        public PendingOrderEventArgs(List<Upbit_Order.PendingOrder> pending_orders, string message)
        {
            pendingOrders = pending_orders;
            Message = message;
        }
    }

    // 호가 조회 이벤트
    public class OrderbookEventArgs : EventArgs
    {
        public Upbit_Orderbook.Orderbook Orderbooks { get; }
        public string Message { get; }

        public OrderbookEventArgs(Upbit_Orderbook.Orderbook orderbook, string message)
        {
            Orderbooks = orderbook;
            Message = message;
        }
    }

    #endregion
}
