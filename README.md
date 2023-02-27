# PiercefieldStocksTickDataRetrieval
Creating this to retrieve US Stock tick data from EOD Historical Data (eodhistoricaldata.com) and place it onto my local SQL Server. 
I'm mostly interested in Mega, Large, and Medium US Stocks from NASDAQ and NYSE, so that's all I'm grabbing. This is around 1400 different stock Symbols (Tickers). 
This data will be used so that I can analyze it internally via a local SQL Server I have running from home. 
I'm uploading this code for free on GitHub in hopes that someone else may use it and not need to totally program it themselves.
There are things within this code that can definitely be personalized based on the specific requirements.
With the default settings, I'm guessing this will download about 2.2 billion rows of ticker data and require about 3 TB of data. 
100,000 API calls can be made a day through EOD Historical Data.. Each request for ticket data counts at 5 API calls. You can request 120 days at a time for 1 symbol.
I'm looking for 1 min tick data, so that's what I'm going to be calling the API for. I believe I want it from the last 12 years.
I do want 12 full calendar years of data from Jan 1 - Dec 31, so I will be grabbing slightly more than 12 years of data, as today is 2/25/2023. 
I've found that EOD historical data provides the cheapest option for gathering such data that I'm interested in requiring, however, the access to the data is not great.
Which is exactly why this program is being built. I can request 20,000 120-day (1 min interval)/day, but TBH my internet isn't fantastic, being out in the middle
of nowhere Alaska. I have two sources of internet, AT&T business and Starlink. The AT&T business is much slower but more reliable, it's a MOFI connection. 
The Starlink connection is much faster, but not as reliable, at the moment the connection from Starlink goes out around every 7 minutes...
So, I'm not enthused about either scenario to be honest. 
However, I'm thinking with if I add enough error checking, then Starlink will be the way to go. As I'm going to be able to download the data much faster..
I can only make 20,000 API requests per day and need about 52,000 overall to do the job.. 
Which means, I can complete the job in 3 days, as long as my network throughput will handle the 2-3 TB of data.
