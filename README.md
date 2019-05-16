# PromotionProcess
This program reads data and created promotion coupon records.

This is a production program so the app.config file has been purposely excluded.

The program takes one parameter, program name, which is required. The parameter will either generate promotion coupon or generates a promotion history record to prevent usage.

The program uses a sql server store procedure to gather the data that requires processing. There is additional logic to exclude 
records that the store procedure could not do. If the record passes all the criteria it will create a promotion coupon record in a sql server table. This program leverages our internal API utility services. 


