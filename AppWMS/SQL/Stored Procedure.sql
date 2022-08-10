USE [ChongQingGZYMESDB]
GO
/****** Object:  StoredProcedure [dbo].[app_wms_stock_sel]    Script Date: 2022/5/17 9:00:59 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- =============================================
-- Author:		zhj
-- Create date: 2022-5-13
-- Description:	库位屏: 库位信息查询
-- =============================================
ALTER PROCEDURE [dbo].[app_wms_stock_sel]
    WITH
    EXEC AS CALLER
AS
begin
--  60个 左右侧 立库
   SELECT s.locationcode,
		  s.[storedtypeid],
		  st.[typename],
		  CASE WHEN s.[storedtypeid] IS NULL THEN '无托盘'
			   WHEN st.[typename] = '原料' THEN bm.[name]
			   WHEN st.[typename] = '产品' THEN bp.[name]
		  ELSE st.[typename] END AS 'stockname',
		  s.[materialid],
		  s.[serialnumber],
          CONVERT (NVARCHAR (100), s.[optime], 120) AS 'optime'
   FROM [dbo].[wms_stock] s WITH (NOLOCK)
        LEFT JOIN [dbo].[wms_storedtype] st ON st.[id] = s.[storedtypeid]
		LEFT JOIN [dbo].[bas_material] bm ON bm.[id] = s.[materialid]
		LEFT JOIN [dbo].[bas_product] bp ON bp.[productcode] = LEFT(s.[serialnumber],LEN(bp.[productcode]))
	WHERE s.[location] NOT IN ('21','22')
   ORDER BY s.[location] 

   --  中间的 61,62
   SELECT s.locationcode,
		  s.[storedtypeid],
		  st.[typename],
		  CASE WHEN s.[storedtypeid] IS NULL THEN '无托盘'
			   WHEN st.[typename] = '原料' THEN bm.[name]
			   WHEN st.[typename] = '产品' THEN bp.[name]
		  ELSE st.[typename] END AS 'stockname',
		  s.[materialid],
		  s.[serialnumber],
          CONVERT (NVARCHAR (100), s.[optime], 120) AS 'optime'
   FROM [dbo].[wms_stock] s WITH (NOLOCK)
        LEFT JOIN [dbo].[wms_storedtype] st ON st.[id] = s.[storedtypeid]
		LEFT JOIN [dbo].[bas_material] bm ON bm.[id] = s.[materialid]
		LEFT JOIN [dbo].[bas_product] bp ON bp.[productcode] = LEFT(s.[serialnumber],LEN(bp.[productcode]))
	WHERE s.[location] IN ('21','22')
   ORDER BY s.[location] 
end