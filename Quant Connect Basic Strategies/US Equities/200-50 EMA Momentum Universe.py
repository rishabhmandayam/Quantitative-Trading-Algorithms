class EMAMomentumUniverse(QCAlgorithm):
    
    def Initialize(self):
        self.SetStartDate(2019, 1, 7)
        self.SetEndDate(2019, 4, 1)
        self.SetCash(100000)
        self.UniverseSettings.Resolution = Resolution.Daily
        self.AddUniverse(self.CoarseSelectionFunction) 
        self.averages = { }
    
    def CoarseSelectionFunction(self, universe):  
        selected = []
        universe = sorted(universe, key=lambda c: c.DollarVolume, reverse=True)  
        universe = [c for c in universe if c.Price > 10][:100]

        for coarse in universe:  
            symbol = coarse.Symbol
            
            if symbol not in self.averages:
                # 1. Call history to get an array of 200 days of history data
                history = self.History(symbol, 200, Resolution.Daily)
                
                #2. Adjust SelectionData to pass in the history result
                self.averages[symbol] = SelectionData(history) 

            self.averages[symbol].update(self.Time, coarse.AdjustedPrice)
            
            if  self.averages[symbol].is_ready() and self.averages[symbol].fast > self.averages[symbol].slow:
                selected.append(symbol)
        
        return selected[:10]
        
    def OnSecuritiesChanged(self, changes):
        for security in changes.RemovedSecurities:
            self.Liquidate(security.Symbol)
       
        for security in changes.AddedSecurities:
            self.SetHoldings(security.Symbol, 0.10)
            
class SelectionData():
    #3. Update the constructor to accept a history array
    def __init__(self, history):
        self.slow = ExponentialMovingAverage(200)
        self.fast = ExponentialMovingAverage(50)
        #4. Loop over the history data and update the indicators
        for bar in history.itertuples():
            self.fast.Update(bar.Index[1], bar.close)
            self.slow.Update(bar.Index[1], bar.close)
            
    def is_ready(self):
        return self.slow.IsReady and self.fast.IsReady
    
    def update(self, time, price):
        self.fast.Update(time, price)
        self.slow.Update(time, price)
  
