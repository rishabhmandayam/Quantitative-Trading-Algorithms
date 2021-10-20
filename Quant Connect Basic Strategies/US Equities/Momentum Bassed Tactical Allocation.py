#Moving Portfolio buying power between stocks and bonds based on market indicators
class MomentumBasedTacticalAllocation(QCAlgorithm):
    
    def Initialize(self):
        
        self.SetStartDate(2007, 8, 1) 
        self.SetEndDate(2010, 8, 1)  
        self.SetCash(3000)  
        
        self.spy = self.AddEquity("SPY", Resolution.Daily)#stocks(more risky)
        self.bnd = self.AddEquity("BND", Resolution.Daily)#bonds(less risky)
      
        self.spyMomentum = self.MOMP("SPY", 50, Resolution.Daily)#Momentum
        self.bondMomentum = self.MOMP("BND", 50, Resolution.Daily) 
       
        self.SetBenchmark(self.spy.Symbol)  
        self.SetWarmUp(50) 
  
    def OnData(self, data):
        
        if self.IsWarmingUp:
            return
        
        #1. Limit trading to happen once per week
        if not self.Time.weekday() == 1: #only wednesday
            return
        
        if self.spyMomentum.Current.Value > self.bondMomentum.Current.Value:
            self.Liquidate("BND")
            self.SetHoldings("SPY", 1)
            
        else:
            self.Liquidate("SPY")
            self.SetHoldings("BND", 1)
            


