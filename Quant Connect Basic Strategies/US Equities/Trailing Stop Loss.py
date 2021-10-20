class BootCampTask(QCAlgorithm):
    
    # Order ticket for our stop order, Datetime when stop order was last hit
    stopMarketTicket = None
    stopMarketOrderFillTime = datetime.min
    highestSPYPrice = 0
    
    def Initialize(self):
        self.SetStartDate(2018, 12, 1)
        self.SetEndDate(2018, 12, 10)
        self.SetCash(100000)
        spy = self.AddEquity("SPY", Resolution.Daily)
        spy.SetDataNormalizationMode(DataNormalizationMode.Raw)
        
    def OnData(self, data):
        
        if (self.Time - self.stopMarketOrderFillTime).days < 15:#Dont re-enter market until after 2 weeks since last loss stop
            return

        if not self.Portfolio.Invested:
            self.MarketOrder("SPY", 500)
            self.stopMarketTicket = self.StopMarketOrder("SPY", -500, 0.9 * self.Securities["SPY"].Close) #liquidate if price dips below 90$
        
        else:
            
            #1. Check if the SPY price is higher that highestSPYPrice.
                if (self.Securities["SPY"].Close > self.highestSPYPrice):
                #2. Save the new high to highestSPYPrice; then update the stop price to 90% of highestSPYPrice 
                    self.highestSPYPrice = self.Securities["SPY"].Close
                    # Update stop loss price using UpdateOrderFields helper.
                    updateFields = UpdateOrderFields()
                    updateFields.StopPrice = self.Securities["SPY"].Close * 0.9
                    self.stopMarketTicket.Update(updateFields)
                #3. Print the new stop price with Debug()
                    self.Debug("New Stop Price: " + str(updateFields.StopPrice))
    def OnOrderEvent(self, orderEvent):
        if orderEvent.Status != OrderStatus.Filled:
            return
        if self.stopMarketTicket is not None and self.stopMarketTicket.OrderId == orderEvent.OrderId: #get time and date of order stop loss order
            self.stopMarketOrderFillTime = self.Time
