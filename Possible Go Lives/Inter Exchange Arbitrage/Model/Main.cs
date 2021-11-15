namespace QuantConnect.Algorithm.CSharp
{
    public class InterExchangeArbitrageAlgorithm : QCAlgorithm
    {
		private Symbol _symbol;
		private decimal _fees;
		private ExchangeMapper _exchangeMapper = new ExchangeMapper();
		
        public override void Initialize()
        {
            SetStartDate(2021, 1, 1);
            SetEndDate(2021, 1, 31);
            SetCash(5000);
            SetBrokerageModel(BrokerageName.Default);
            
            var security = AddEquity("TSLA", Resolution.Tick);
            security.SetFillModel(new CustomFillModel(this));
            _symbol = security.Symbol;
            
            _fees = 1m; // Atreyu fees are $0.0015/share 
        }
		
        public override void OnData(Slice data)
        {
            // Gather quote ticks for each exchange
            var quotes = data.Ticks[_symbol].Where(tick => tick.TickType == TickType.Quote && _exchangeMapper.ContainsKey(tick.Exchange));
            //filter out quotes with price of zero
            var sellQuotes = quotes.Where(quote => quote.AskPrice != 0); 
            var buyQuotes = quotes.Where(quote => quote.BidPrice != 0);
            
            // Check if there are both buy and sell quotes
            if (sellQuotes.Count() == 0 || buyQuotes.Count() == 0)
            {
            	return;
            }
            	
            // Find lowest sell quote
            var lowestSellQuote = sellQuotes.OrderBy(quote => quote.AskPrice).First();
            
            // Find highest buy quote
            var highestBuyQuote = buyQuotes.OrderByDescending(quote => quote.BidPrice).First();
            
            // Check if there is an arbitrage opportunity
            if (lowestSellQuote.AskPrice >= highestBuyQuote.BidPrice - _fees)
            {
            	return;
            }
            
            // Determine order size
            var quantity = Math.Min(lowestSellQuote.AskSize, highestBuyQuote.BidSize);
            quantity = Math.Min(quantity, CalculateOrderQuantity(_symbol, 1.0m));
            
            // Buy from underpriced exchange
            var orderProperties = new AtreyuOrderProperties();
            orderProperties.Exchange = _exchangeMapper.getExchangeFromName(lowestSellQuote.Exchange);
            MarketOrder(_symbol, quantity, true, $"{lowestSellQuote.Exchange}", orderProperties);
            
            // Sell on overpriced exchange
            orderProperties.Exchange = _exchangeMapper.getExchangeFromName(highestBuyQuote.Exchange);
            MarketOrder(_symbol, -quantity, true, $"{highestBuyQuote.Exchange}", orderProperties=orderProperties);
        }


		internal class CustomFillModel : FillModel
        {
        	private readonly QCAlgorithm _algorithm;
        	private ExchangeMapper _exchangeMapper = new ExchangeMapper();
        	
        	public CustomFillModel(QCAlgorithm algorithm)
                : base()
            {
                _algorithm = algorithm;
            }
        	
            public override OrderEvent MarketFill(Security asset, MarketOrder order)
            {
                var fill = new OrderEvent(order, order.Time, OrderFee.Zero);
                
                var quotes = asset.Cache.GetAll<Tick>().Where(tick => tick.TickType == TickType.Quote && _exchangeMapper.ContainsKey(tick.Exchange));
                
                if (order.Quantity > 0) // Buy order
                {
                	// Set fill price to price of lowest sell quote
					fill.FillPrice = quotes.Where(quote => quote.AskPrice != 0)
										   .OrderBy(quote => quote.AskPrice)
										   .First()
										   .AskPrice;
                } 
                else // Sell order
                {
                	// Set fill price to price of highest bid quote
                	fill.FillPrice = quotes.Where(quote => quote.BidPrice != 0)
                						   .OrderByDescending(quote => quote.BidPrice)
                						   .First()
                						   .BidPrice;
                }
                
                fill.FillQuantity = order.Quantity;
                fill.Status = OrderStatus.Filled;
                return fill;
            }
        }

		internal class ExchangeMapper
		{
			private Dictionary<string, Exchange> _exchangeByName = new Dictionary<string, Exchange>()
			{
				{"NASDAQ", Exchange.NASDAQ},
				{"BATS", Exchange.BATS},
				{"ARCA", Exchange.ARCA},
				{"NYSE", Exchange.NYSE},
				{"NSX", Exchange.NSX},
				{"FINRA", Exchange.FINRA},
				{"ISE", Exchange.ISE},
				{"CSE", Exchange.CSE},
				{"CBOE", Exchange.CBOE},
				{"NASDAQ_BX", Exchange.NASDAQ_BX},
				{"SIAC", Exchange.SIAC},
				{"EDGA", Exchange.EDGA},
				{"EDGX", Exchange.EDGX},
				{"NASDAQ_PSX", Exchange.NASDAQ_PSX},
				{"BATS_Y", Exchange.BATS_Y},
				{"BOSTON", Exchange.BOSTON},
				{"AMEX", Exchange.AMEX},
				{"BSE", Exchange.BSE},
				{"NSE", Exchange.NSE}
			};
			
			public Exchange getExchangeFromName(string name)
			{
				return _exchangeByName[name];
			}
			
			public bool ContainsKey(string key)
			{
				return _exchangeByName.ContainsKey(key);
			}
		}
    }
}