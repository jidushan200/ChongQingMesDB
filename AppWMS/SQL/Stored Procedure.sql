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
-- Description:	��λ��: ��λ��Ϣ��ѯ
-- =============================================
ALTER PROCEDURE [dbo].[app_wms_stock_sel]
    WITH
    EXEC AS CALLER
AS
begin
--  60�� ���Ҳ� ����
   SELECT s.locationcode,
		  s.[storedtypeid],
		  st.[typename],
		  CASE WHEN s.[storedtypeid] IS NULL THEN '������'
			   WHEN st.[typename] = 'ԭ��' THEN bm.[name]
			   WHEN st.[typename] = '��Ʒ' THEN bp.[name]
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

   --  �м�� 61,62
   SELECT s.locationcode,
		  s.[storedtypeid],
		  st.[typename],
		  CASE WHEN s.[storedtypeid] IS NULL THEN '������'
			   WHEN st.[typename] = 'ԭ��' THEN bm.[name]
			   WHEN st.[typename] = '��Ʒ' THEN bp.[name]
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