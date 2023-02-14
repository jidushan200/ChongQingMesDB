USE [master]
GO
/****** Object:  Database [ChongQingGZYMESDB]    Script Date: 2022/5/17 9:07:49 ******/
CREATE DATABASE [ChongQingGZYMESDB]
 CONTAINMENT = NONE
 ON  PRIMARY 
( NAME = N'ChongQingGZYMESDB', FILENAME = N'D:\Program Files\Microsoft SQL Server\MSSQL13.MSSQLSERVER\MSSQL\DATA\ChongQingGZYMESDB.mdf' , SIZE = 8192KB , MAXSIZE = UNLIMITED, FILEGROWTH = 65536KB )
 LOG ON 
( NAME = N'ChongQingGZYMESDB_log', FILENAME = N'D:\Program Files\Microsoft SQL Server\MSSQL13.MSSQLSERVER\MSSQL\DATA\ChongQingGZYMESDB_log.ldf' , SIZE = 8192KB , MAXSIZE = 2048GB , FILEGROWTH = 65536KB )
GO
ALTER DATABASE [ChongQingGZYMESDB] SET COMPATIBILITY_LEVEL = 130
GO
IF (1 = FULLTEXTSERVICEPROPERTY('IsFullTextInstalled'))
begin
EXEC [ChongQingGZYMESDB].[dbo].[sp_fulltext_database] @action = 'enable'
end
GO
ALTER DATABASE [ChongQingGZYMESDB] SET ANSI_NULL_DEFAULT OFF 
GO
ALTER DATABASE [ChongQingGZYMESDB] SET ANSI_NULLS OFF 
GO
ALTER DATABASE [ChongQingGZYMESDB] SET ANSI_PADDING OFF 
GO
ALTER DATABASE [ChongQingGZYMESDB] SET ANSI_WARNINGS OFF 
GO
ALTER DATABASE [ChongQingGZYMESDB] SET ARITHABORT OFF 
GO
ALTER DATABASE [ChongQingGZYMESDB] SET AUTO_CLOSE OFF 
GO
ALTER DATABASE [ChongQingGZYMESDB] SET AUTO_SHRINK OFF 
GO
ALTER DATABASE [ChongQingGZYMESDB] SET AUTO_UPDATE_STATISTICS ON 
GO
ALTER DATABASE [ChongQingGZYMESDB] SET CURSOR_CLOSE_ON_COMMIT OFF 
GO
ALTER DATABASE [ChongQingGZYMESDB] SET CURSOR_DEFAULT  GLOBAL 
GO
ALTER DATABASE [ChongQingGZYMESDB] SET CONCAT_NULL_YIELDS_NULL OFF 
GO
ALTER DATABASE [ChongQingGZYMESDB] SET NUMERIC_ROUNDABORT OFF 
GO
ALTER DATABASE [ChongQingGZYMESDB] SET QUOTED_IDENTIFIER OFF 
GO
ALTER DATABASE [ChongQingGZYMESDB] SET RECURSIVE_TRIGGERS OFF 
GO
ALTER DATABASE [ChongQingGZYMESDB] SET  DISABLE_BROKER 
GO
ALTER DATABASE [ChongQingGZYMESDB] SET AUTO_UPDATE_STATISTICS_ASYNC OFF 
GO
ALTER DATABASE [ChongQingGZYMESDB] SET DATE_CORRELATION_OPTIMIZATION OFF 
GO
ALTER DATABASE [ChongQingGZYMESDB] SET TRUSTWORTHY OFF 
GO
ALTER DATABASE [ChongQingGZYMESDB] SET ALLOW_SNAPSHOT_ISOLATION OFF 
GO
ALTER DATABASE [ChongQingGZYMESDB] SET PARAMETERIZATION SIMPLE 
GO
ALTER DATABASE [ChongQingGZYMESDB] SET READ_COMMITTED_SNAPSHOT OFF 
GO
ALTER DATABASE [ChongQingGZYMESDB] SET HONOR_BROKER_PRIORITY OFF 
GO
ALTER DATABASE [ChongQingGZYMESDB] SET RECOVERY FULL 
GO
ALTER DATABASE [ChongQingGZYMESDB] SET  MULTI_USER 
GO
ALTER DATABASE [ChongQingGZYMESDB] SET PAGE_VERIFY CHECKSUM  
GO
ALTER DATABASE [ChongQingGZYMESDB] SET DB_CHAINING OFF 
GO
ALTER DATABASE [ChongQingGZYMESDB] SET FILESTREAM( NON_TRANSACTED_ACCESS = OFF ) 
GO
ALTER DATABASE [ChongQingGZYMESDB] SET TARGET_RECOVERY_TIME = 60 SECONDS 
GO
ALTER DATABASE [ChongQingGZYMESDB] SET DELAYED_DURABILITY = DISABLED 
GO
EXEC sys.sp_db_vardecimal_storage_format N'ChongQingGZYMESDB', N'ON'
GO
ALTER DATABASE [ChongQingGZYMESDB] SET QUERY_STORE = OFF
GO
USE [ChongQingGZYMESDB]
GO
ALTER DATABASE SCOPED CONFIGURATION SET LEGACY_CARDINALITY_ESTIMATION = OFF;
GO
ALTER DATABASE SCOPED CONFIGURATION SET MAXDOP = 0;
GO
ALTER DATABASE SCOPED CONFIGURATION SET PARAMETER_SNIFFING = ON;
GO
ALTER DATABASE SCOPED CONFIGURATION SET QUERY_OPTIMIZER_HOTFIXES = OFF;
GO
USE [ChongQingGZYMESDB]
GO
/****** Object:  Table [dbo].[agv_comm]    Script Date: 2022/5/17 9:07:49 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[agv_comm](
	[name] [nvarchar](20) NOT NULL,
	[value] [nvarchar](20) NOT NULL,
	[optime] [datetime] NOT NULL,
 CONSTRAINT [PK_agv_comm] PRIMARY KEY CLUSTERED 
(
	[name] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[agv_sn]    Script Date: 2022/5/17 9:07:49 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[agv_sn](
	[robotcode] [nvarchar](20) NOT NULL,
	[materialid] [int] NULL,
	[productid] [int] NULL,
	[optime] [datetime] NOT NULL,
 CONSTRAINT [PK_agv_sn] PRIMARY KEY CLUSTERED 
(
	[robotcode] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[agv_state]    Script Date: 2022/5/17 9:07:49 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[agv_state](
	[robotcode] [nvarchar](20) NOT NULL,
	[robotip] [nvarchar](20) NOT NULL,
	[statusstr] [nvarchar](20) NOT NULL,
	[excludestr] [nvarchar](20) NOT NULL,
	[stopstr] [nvarchar](20) NOT NULL,
	[direction] [int] NOT NULL,
	[x] [int] NOT NULL,
	[y] [int] NOT NULL,
	[batterylevel] [int] NOT NULL,
	[optime] [datetime] NOT NULL,
 CONSTRAINT [PK_agv_state] PRIMARY KEY CLUSTERED 
(
	[robotcode] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[agv_task]    Script Date: 2022/5/17 9:07:49 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[agv_task](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[taskcode] [nvarchar](50) NOT NULL,
	[tasktype] [nvarchar](20) NOT NULL,
	[robotcode] [nvarchar](20) NULL,
	[srccode] [nvarchar](20) NULL,
	[destcode] [nvarchar](20) NULL,
	[callbacktime] [datetime] NULL,
	[cmd] [int] NULL,
	[sendtime] [datetime] NULL,
 CONSTRAINT [PK_agv_task] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[agv_tcp]    Script Date: 2022/5/17 9:07:49 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[agv_tcp](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[robotcode] [nvarchar](20) NOT NULL,
	[ordertype] [int] NOT NULL,
	[optime] [datetime] NOT NULL,
 CONSTRAINT [PK_agv_tcp] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[bas_comm]    Script Date: 2022/5/17 9:07:49 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[bas_comm](
	[sectionname] [nvarchar](20) NOT NULL,
	[name] [nvarchar](20) NOT NULL,
	[ordernumber] [int] NULL,
	[value] [nvarchar](20) NULL,
	[optime] [datetime] NOT NULL,
	[description] [nvarchar](50) NULL,
 CONSTRAINT [PK_bas_comm] PRIMARY KEY CLUSTERED 
(
	[sectionname] ASC,
	[name] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[bas_material]    Script Date: 2022/5/17 9:07:49 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[bas_material](
	[id] [int] NOT NULL,
	[name] [nvarchar](20) NULL,
	[pencupproductid] [int] NULL,
 CONSTRAINT [PK_bas_material] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[bas_product]    Script Date: 2022/5/17 9:07:49 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[bas_product](
	[id] [int] NOT NULL,
	[name] [nvarchar](50) NULL,
	[materialid] [int] NOT NULL,
 CONSTRAINT [PK_bas_product] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[bas_station]    Script Date: 2022/5/17 9:07:49 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[bas_station](
	[code] [nvarchar](20) NOT NULL,
	[description] [nvarchar](50) NULL,
	[timestamp] [datetime] NOT NULL,
 CONSTRAINT [PK_bas_station] PRIMARY KEY CLUSTERED 
(
	[code] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[plc_datakeep]    Script Date: 2022/5/17 9:07:49 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[plc_datakeep](
	[ip] [int] IDENTITY(1,1) NOT NULL,
	[stationcode] [nvarchar](20) NULL,
	[stationorder] [nvarchar](50) NULL,
	[lasttime] [datetime] NULL,
 CONSTRAINT [PK_plc_datakeep] PRIMARY KEY CLUSTERED 
(
	[ip] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[plc_dataset]    Script Date: 2022/5/17 9:07:49 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[plc_dataset](
	[channel] [nvarchar](20) NOT NULL,
	[device] [nvarchar](20) NOT NULL,
	[taggroup] [nvarchar](50) NOT NULL,
	[tag] [nvarchar](20) NOT NULL,
	[datatype] [nvarchar](20) NOT NULL,
	[val] [nvarchar](100) NULL,
	[lasttime] [datetime] NULL,
 CONSTRAINT [PK_plc_dataset] PRIMARY KEY CLUSTERED 
(
	[channel] ASC,
	[device] ASC,
	[taggroup] ASC,
	[tag] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[plc_datatag]    Script Date: 2022/5/17 9:07:49 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[plc_datatag](
	[channel] [nvarchar](20) NOT NULL,
	[device] [nvarchar](20) NOT NULL,
	[taggroup] [nvarchar](20) NOT NULL,
	[tag] [nvarchar](50) NOT NULL,
	[id] [int] NULL,
	[datachangeignore] [int] NULL,
	[datachangsql] [nvarchar](50) NULL,
	[getdatasql] [nvarchar](50) NULL,
	[validvalues] [nvarchar](50) NULL,
	[getdatapid] [int] NULL,
	[type] [int] NULL,
	[code] [nvarchar](50) NULL,
	[desc] [nvarchar](50) NULL,
	[stationcode] [nvarchar](50) NULL,
 CONSTRAINT [PK_plc_datatag] PRIMARY KEY CLUSTERED 
(
	[channel] ASC,
	[device] ASC,
	[taggroup] ASC,
	[tag] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[sch_order]    Script Date: 2022/5/17 9:07:49 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[sch_order](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[prioritynumber] [int] NULL,
	[productid] [int] NULL,
	[ordernumber] [nvarchar](20) NULL,
	[quantity] [int] NULL,
	[onlinecnt] [int] NULL,
	[finishedcnt] [int] NULL,
 CONSTRAINT [PK_sch_order] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[sch_ordercnc]    Script Date: 2022/5/17 9:07:49 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[sch_ordercnc](
	[id] [int] NOT NULL,
	[prioritynumber] [int] NOT NULL,
	[productid] [int] NOT NULL,
	[ordernumber] [nvarchar](20) NOT NULL,
	[quantity] [int] NOT NULL,
	[onlinecnt] [int] NOT NULL,
	[finishedcnt] [int] NOT NULL,
 CONSTRAINT [PK_sch_cncorder] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[sch_running]    Script Date: 2022/5/17 9:07:49 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[sch_running](
	[id] [int] NOT NULL,
	[state] [int] NOT NULL,
	[timestamp] [datetime] NOT NULL,
	[exectimestamp] [datetime] NOT NULL,
	[sysstop] [int] NULL,
 CONSTRAINT [PK_sch_running] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[sch_tracking]    Script Date: 2022/5/17 9:07:49 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[sch_tracking](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[stationcode] [nvarchar](20) NOT NULL,
	[action] [nvarchar](20) NOT NULL,
	[serialnumber] [nvarchar](50) NOT NULL,
	[optime] [datetime] NOT NULL,
 CONSTRAINT [PK_sch_tracking] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[wms_pallet]    Script Date: 2022/5/17 9:07:49 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[wms_pallet](
	[id] [int] NOT NULL,
	[productSN] [nvarchar](30) NULL,
	[pencupSN] [nvarchar](30) NULL,
	[timestamp] [datetime] NOT NULL,
 CONSTRAINT [PK_wms_pallet] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[wms_stock]    Script Date: 2022/5/17 9:07:49 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[wms_stock](
	[id] [int] NOT NULL,
	[locationid] [int] NOT NULL,
	[storedtypeid] [int] NULL,
	[materialid] [int] NULL,
	[productSN] [nvarchar](30) NULL,
	[pencupSN] [nvarchar](30) NULL,
	[optime] [datetime] NOT NULL,
 CONSTRAINT [PK_wms_stock] PRIMARY KEY CLUSTERED 
(
	[id] ASC,
	[locationid] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
INSERT [dbo].[agv_sn] ([robotcode], [materialid], [productid], [optime]) VALUES (N'0001', NULL, NULL, CAST(N'2022-05-09T08:47:33.000' AS DateTime))
INSERT [dbo].[agv_sn] ([robotcode], [materialid], [productid], [optime]) VALUES (N'0002', NULL, NULL, CAST(N'2022-05-09T08:47:43.000' AS DateTime))
INSERT [dbo].[agv_sn] ([robotcode], [materialid], [productid], [optime]) VALUES (N'0003', NULL, NULL, CAST(N'2022-05-09T08:48:06.000' AS DateTime))
GO
INSERT [dbo].[bas_material] ([id], [name], [pencupproductid]) VALUES (1, N'红色毛坯料', NULL)
INSERT [dbo].[bas_material] ([id], [name], [pencupproductid]) VALUES (2, N'绿色毛坯料', NULL)
INSERT [dbo].[bas_material] ([id], [name], [pencupproductid]) VALUES (3, N'底座', NULL)
INSERT [dbo].[bas_material] ([id], [name], [pencupproductid]) VALUES (4, N'手机支架', NULL)
GO
INSERT [dbo].[bas_product] ([id], [name], [materialid]) VALUES (1, N'红色笔筒', 1)
INSERT [dbo].[bas_product] ([id], [name], [materialid]) VALUES (2, N'绿色笔筒', 2)
INSERT [dbo].[bas_product] ([id], [name], [materialid]) VALUES (3, N'办公套件1', 1)
INSERT [dbo].[bas_product] ([id], [name], [materialid]) VALUES (4, N'办公套件2', 2)
GO
INSERT [dbo].[bas_station] ([code], [description], [timestamp]) VALUES (N'CNCC1', N'加工中心停泊位C1', CAST(N'2022-05-09T14:41:52.000' AS DateTime))
INSERT [dbo].[bas_station] ([code], [description], [timestamp]) VALUES (N'CNCC2', N'加工中心停泊位C2', CAST(N'2022-05-09T14:42:34.000' AS DateTime))
INSERT [dbo].[bas_station] ([code], [description], [timestamp]) VALUES (N'OP01', N'环线1工位', CAST(N'2022-05-13T08:54:24.000' AS DateTime))
INSERT [dbo].[bas_station] ([code], [description], [timestamp]) VALUES (N'OP02', N'环线2工位', CAST(N'2022-05-13T08:54:40.000' AS DateTime))
INSERT [dbo].[bas_station] ([code], [description], [timestamp]) VALUES (N'OP03', N'环线3工位', CAST(N'2022-05-13T08:55:08.000' AS DateTime))
INSERT [dbo].[bas_station] ([code], [description], [timestamp]) VALUES (N'OP04', N'环线4工位', CAST(N'2022-05-13T08:55:34.000' AS DateTime))
INSERT [dbo].[bas_station] ([code], [description], [timestamp]) VALUES (N'OP05', N'环线5工位', CAST(N'2022-05-13T08:55:52.000' AS DateTime))
INSERT [dbo].[bas_station] ([code], [description], [timestamp]) VALUES (N'OP06', N'环线6工位', CAST(N'2022-05-13T08:56:10.000' AS DateTime))
INSERT [dbo].[bas_station] ([code], [description], [timestamp]) VALUES (N'OP07', N'环线7工位', CAST(N'2022-05-13T08:56:25.000' AS DateTime))
INSERT [dbo].[bas_station] ([code], [description], [timestamp]) VALUES (N'OP08', N'环线8工位', CAST(N'2022-05-13T08:56:40.000' AS DateTime))
INSERT [dbo].[bas_station] ([code], [description], [timestamp]) VALUES (N'OP09', N'环线9工位', CAST(N'2022-05-13T08:56:54.000' AS DateTime))
INSERT [dbo].[bas_station] ([code], [description], [timestamp]) VALUES (N'OP10', N'环线10工位', CAST(N'2022-05-13T08:57:11.000' AS DateTime))
INSERT [dbo].[bas_station] ([code], [description], [timestamp]) VALUES (N'WMS1A', N'毛坯库出库停泊点A', CAST(N'2022-05-09T14:39:59.000' AS DateTime))
INSERT [dbo].[bas_station] ([code], [description], [timestamp]) VALUES (N'WMS1B', N'毛坯库入库停泊点B', CAST(N'2022-05-09T14:40:56.000' AS DateTime))
INSERT [dbo].[bas_station] ([code], [description], [timestamp]) VALUES (N'WMS2D1', N'配件立库停泊点D1', CAST(N'2022-05-09T14:43:55.000' AS DateTime))
INSERT [dbo].[bas_station] ([code], [description], [timestamp]) VALUES (N'WMS2D2', N'配件立库停泊位D2', CAST(N'2022-05-09T14:44:35.000' AS DateTime))
GO
INSERT [dbo].[wms_pallet] ([id], [productSN], [pencupSN], [timestamp]) VALUES (1, NULL, NULL, CAST(N'2022-05-13T10:37:14.000' AS DateTime))
INSERT [dbo].[wms_pallet] ([id], [productSN], [pencupSN], [timestamp]) VALUES (2, NULL, NULL, CAST(N'2022-05-13T10:37:41.000' AS DateTime))
GO
INSERT [dbo].[wms_stock] ([id], [locationid], [storedtypeid], [materialid], [productSN], [pencupSN], [optime]) VALUES (1, 1, 0, NULL, NULL, N'0', CAST(N'2022-05-09T14:46:49.000' AS DateTime))
INSERT [dbo].[wms_stock] ([id], [locationid], [storedtypeid], [materialid], [productSN], [pencupSN], [optime]) VALUES (1, 2, 0, NULL, NULL, N'0', CAST(N'2022-05-09T14:47:29.000' AS DateTime))
INSERT [dbo].[wms_stock] ([id], [locationid], [storedtypeid], [materialid], [productSN], [pencupSN], [optime]) VALUES (1, 3, 0, NULL, NULL, N'0', CAST(N'2022-05-09T14:47:46.000' AS DateTime))
INSERT [dbo].[wms_stock] ([id], [locationid], [storedtypeid], [materialid], [productSN], [pencupSN], [optime]) VALUES (1, 4, 0, NULL, NULL, N'0', CAST(N'2022-05-09T14:48:05.000' AS DateTime))
INSERT [dbo].[wms_stock] ([id], [locationid], [storedtypeid], [materialid], [productSN], [pencupSN], [optime]) VALUES (1, 5, 0, NULL, NULL, N'0', CAST(N'2022-05-09T14:48:36.000' AS DateTime))
INSERT [dbo].[wms_stock] ([id], [locationid], [storedtypeid], [materialid], [productSN], [pencupSN], [optime]) VALUES (1, 6, 0, NULL, NULL, N'0', CAST(N'2022-05-09T14:49:05.000' AS DateTime))
INSERT [dbo].[wms_stock] ([id], [locationid], [storedtypeid], [materialid], [productSN], [pencupSN], [optime]) VALUES (1, 7, 0, NULL, NULL, N'0', CAST(N'2022-05-09T14:49:17.000' AS DateTime))
INSERT [dbo].[wms_stock] ([id], [locationid], [storedtypeid], [materialid], [productSN], [pencupSN], [optime]) VALUES (1, 8, 0, NULL, NULL, N'0', CAST(N'2022-05-09T14:49:32.000' AS DateTime))
INSERT [dbo].[wms_stock] ([id], [locationid], [storedtypeid], [materialid], [productSN], [pencupSN], [optime]) VALUES (1, 9, 0, NULL, NULL, N'0', CAST(N'2022-05-09T14:50:22.000' AS DateTime))
INSERT [dbo].[wms_stock] ([id], [locationid], [storedtypeid], [materialid], [productSN], [pencupSN], [optime]) VALUES (1, 10, 0, NULL, NULL, N'0', CAST(N'2022-05-09T14:50:41.000' AS DateTime))
INSERT [dbo].[wms_stock] ([id], [locationid], [storedtypeid], [materialid], [productSN], [pencupSN], [optime]) VALUES (1, 11, 0, NULL, NULL, N'0', CAST(N'2022-05-09T14:50:54.000' AS DateTime))
INSERT [dbo].[wms_stock] ([id], [locationid], [storedtypeid], [materialid], [productSN], [pencupSN], [optime]) VALUES (1, 12, 0, NULL, NULL, N'0', CAST(N'2022-05-09T14:51:07.000' AS DateTime))
INSERT [dbo].[wms_stock] ([id], [locationid], [storedtypeid], [materialid], [productSN], [pencupSN], [optime]) VALUES (1, 13, 0, NULL, NULL, N'0', CAST(N'2022-05-09T14:51:19.000' AS DateTime))
INSERT [dbo].[wms_stock] ([id], [locationid], [storedtypeid], [materialid], [productSN], [pencupSN], [optime]) VALUES (1, 14, 0, NULL, NULL, N'0', CAST(N'2022-05-09T14:51:35.000' AS DateTime))
INSERT [dbo].[wms_stock] ([id], [locationid], [storedtypeid], [materialid], [productSN], [pencupSN], [optime]) VALUES (1, 15, 0, NULL, NULL, N'0', CAST(N'2022-05-09T14:52:04.000' AS DateTime))
INSERT [dbo].[wms_stock] ([id], [locationid], [storedtypeid], [materialid], [productSN], [pencupSN], [optime]) VALUES (1, 16, 0, NULL, NULL, N'0', CAST(N'2022-05-09T14:52:44.000' AS DateTime))
INSERT [dbo].[wms_stock] ([id], [locationid], [storedtypeid], [materialid], [productSN], [pencupSN], [optime]) VALUES (1, 17, 0, NULL, NULL, N'0', CAST(N'2022-05-09T14:53:07.000' AS DateTime))
INSERT [dbo].[wms_stock] ([id], [locationid], [storedtypeid], [materialid], [productSN], [pencupSN], [optime]) VALUES (1, 18, 0, NULL, NULL, N'0', CAST(N'2022-05-09T14:53:21.000' AS DateTime))
INSERT [dbo].[wms_stock] ([id], [locationid], [storedtypeid], [materialid], [productSN], [pencupSN], [optime]) VALUES (1, 19, 0, NULL, NULL, N'0', CAST(N'2022-05-09T14:53:33.000' AS DateTime))
INSERT [dbo].[wms_stock] ([id], [locationid], [storedtypeid], [materialid], [productSN], [pencupSN], [optime]) VALUES (1, 20, 0, NULL, NULL, N'0', CAST(N'2022-05-09T14:53:45.000' AS DateTime))
INSERT [dbo].[wms_stock] ([id], [locationid], [storedtypeid], [materialid], [productSN], [pencupSN], [optime]) VALUES (1, 21, 0, NULL, NULL, N'0', CAST(N'2022-05-09T14:54:00.000' AS DateTime))
INSERT [dbo].[wms_stock] ([id], [locationid], [storedtypeid], [materialid], [productSN], [pencupSN], [optime]) VALUES (1, 22, 0, NULL, NULL, N'0', CAST(N'2022-05-09T14:54:14.000' AS DateTime))
INSERT [dbo].[wms_stock] ([id], [locationid], [storedtypeid], [materialid], [productSN], [pencupSN], [optime]) VALUES (1, 23, 0, NULL, NULL, N'0', CAST(N'2022-05-09T14:54:26.000' AS DateTime))
INSERT [dbo].[wms_stock] ([id], [locationid], [storedtypeid], [materialid], [productSN], [pencupSN], [optime]) VALUES (1, 24, 0, NULL, NULL, N'0', CAST(N'2022-05-09T14:54:52.000' AS DateTime))
INSERT [dbo].[wms_stock] ([id], [locationid], [storedtypeid], [materialid], [productSN], [pencupSN], [optime]) VALUES (1, 25, 0, NULL, NULL, N'0', CAST(N'2022-05-09T14:55:03.000' AS DateTime))
INSERT [dbo].[wms_stock] ([id], [locationid], [storedtypeid], [materialid], [productSN], [pencupSN], [optime]) VALUES (1, 26, 0, NULL, NULL, N'0', CAST(N'2022-05-09T14:55:33.000' AS DateTime))
INSERT [dbo].[wms_stock] ([id], [locationid], [storedtypeid], [materialid], [productSN], [pencupSN], [optime]) VALUES (1, 27, 0, NULL, NULL, N'0', CAST(N'2022-05-09T14:59:53.000' AS DateTime))
INSERT [dbo].[wms_stock] ([id], [locationid], [storedtypeid], [materialid], [productSN], [pencupSN], [optime]) VALUES (1, 28, 0, NULL, NULL, N'0', CAST(N'2022-05-09T15:00:11.000' AS DateTime))
INSERT [dbo].[wms_stock] ([id], [locationid], [storedtypeid], [materialid], [productSN], [pencupSN], [optime]) VALUES (1, 29, 0, NULL, NULL, N'0', CAST(N'2022-05-09T15:00:29.000' AS DateTime))
INSERT [dbo].[wms_stock] ([id], [locationid], [storedtypeid], [materialid], [productSN], [pencupSN], [optime]) VALUES (1, 30, 0, NULL, NULL, N'0', CAST(N'2022-05-09T15:00:43.000' AS DateTime))
INSERT [dbo].[wms_stock] ([id], [locationid], [storedtypeid], [materialid], [productSN], [pencupSN], [optime]) VALUES (1, 31, 0, NULL, NULL, N'0', CAST(N'2022-05-09T15:11:08.000' AS DateTime))
INSERT [dbo].[wms_stock] ([id], [locationid], [storedtypeid], [materialid], [productSN], [pencupSN], [optime]) VALUES (1, 32, 0, NULL, NULL, N'0', CAST(N'2022-05-09T15:11:28.000' AS DateTime))
INSERT [dbo].[wms_stock] ([id], [locationid], [storedtypeid], [materialid], [productSN], [pencupSN], [optime]) VALUES (1, 33, 0, NULL, NULL, N'0', CAST(N'2022-05-09T15:11:41.000' AS DateTime))
INSERT [dbo].[wms_stock] ([id], [locationid], [storedtypeid], [materialid], [productSN], [pencupSN], [optime]) VALUES (1, 34, 0, NULL, NULL, N'0', CAST(N'2022-05-09T15:12:00.000' AS DateTime))
INSERT [dbo].[wms_stock] ([id], [locationid], [storedtypeid], [materialid], [productSN], [pencupSN], [optime]) VALUES (1, 35, 0, NULL, NULL, N'0', CAST(N'2022-05-09T15:12:15.000' AS DateTime))
INSERT [dbo].[wms_stock] ([id], [locationid], [storedtypeid], [materialid], [productSN], [pencupSN], [optime]) VALUES (1, 36, 0, NULL, NULL, N'0', CAST(N'2022-05-09T15:12:28.000' AS DateTime))
INSERT [dbo].[wms_stock] ([id], [locationid], [storedtypeid], [materialid], [productSN], [pencupSN], [optime]) VALUES (1, 37, 0, NULL, NULL, N'0', CAST(N'2022-05-09T15:12:51.000' AS DateTime))
INSERT [dbo].[wms_stock] ([id], [locationid], [storedtypeid], [materialid], [productSN], [pencupSN], [optime]) VALUES (1, 38, 0, NULL, NULL, N'0', CAST(N'2022-05-09T15:13:07.000' AS DateTime))
INSERT [dbo].[wms_stock] ([id], [locationid], [storedtypeid], [materialid], [productSN], [pencupSN], [optime]) VALUES (1, 39, 0, NULL, NULL, N'0', CAST(N'2022-05-09T15:13:20.000' AS DateTime))
INSERT [dbo].[wms_stock] ([id], [locationid], [storedtypeid], [materialid], [productSN], [pencupSN], [optime]) VALUES (1, 40, 0, NULL, NULL, N'0', CAST(N'2022-05-09T15:13:32.000' AS DateTime))
INSERT [dbo].[wms_stock] ([id], [locationid], [storedtypeid], [materialid], [productSN], [pencupSN], [optime]) VALUES (1, 41, 0, NULL, NULL, N'0', CAST(N'2022-05-09T15:13:58.000' AS DateTime))
INSERT [dbo].[wms_stock] ([id], [locationid], [storedtypeid], [materialid], [productSN], [pencupSN], [optime]) VALUES (1, 42, 0, NULL, NULL, N'0', CAST(N'2022-05-09T15:14:12.000' AS DateTime))
INSERT [dbo].[wms_stock] ([id], [locationid], [storedtypeid], [materialid], [productSN], [pencupSN], [optime]) VALUES (1, 43, 0, NULL, NULL, N'0', CAST(N'2022-05-09T15:14:29.000' AS DateTime))
INSERT [dbo].[wms_stock] ([id], [locationid], [storedtypeid], [materialid], [productSN], [pencupSN], [optime]) VALUES (1, 44, 0, NULL, NULL, N'0', CAST(N'2022-05-09T15:14:43.000' AS DateTime))
INSERT [dbo].[wms_stock] ([id], [locationid], [storedtypeid], [materialid], [productSN], [pencupSN], [optime]) VALUES (1, 45, 0, NULL, NULL, N'0', CAST(N'2022-05-09T15:14:56.000' AS DateTime))
INSERT [dbo].[wms_stock] ([id], [locationid], [storedtypeid], [materialid], [productSN], [pencupSN], [optime]) VALUES (1, 46, 0, NULL, NULL, N'0', CAST(N'2022-05-09T15:15:10.000' AS DateTime))
INSERT [dbo].[wms_stock] ([id], [locationid], [storedtypeid], [materialid], [productSN], [pencupSN], [optime]) VALUES (1, 47, 0, NULL, NULL, N'0', CAST(N'2022-05-09T15:15:24.000' AS DateTime))
INSERT [dbo].[wms_stock] ([id], [locationid], [storedtypeid], [materialid], [productSN], [pencupSN], [optime]) VALUES (1, 48, 0, NULL, NULL, N'0', CAST(N'2022-05-09T15:15:43.000' AS DateTime))
INSERT [dbo].[wms_stock] ([id], [locationid], [storedtypeid], [materialid], [productSN], [pencupSN], [optime]) VALUES (1, 49, 0, NULL, NULL, N'0', CAST(N'2022-05-09T15:16:02.000' AS DateTime))
INSERT [dbo].[wms_stock] ([id], [locationid], [storedtypeid], [materialid], [productSN], [pencupSN], [optime]) VALUES (1, 50, 0, NULL, NULL, N'0', CAST(N'2022-05-09T15:16:14.000' AS DateTime))
INSERT [dbo].[wms_stock] ([id], [locationid], [storedtypeid], [materialid], [productSN], [pencupSN], [optime]) VALUES (1, 51, 0, NULL, NULL, N'0', CAST(N'2022-05-09T15:16:29.000' AS DateTime))
INSERT [dbo].[wms_stock] ([id], [locationid], [storedtypeid], [materialid], [productSN], [pencupSN], [optime]) VALUES (1, 52, 0, NULL, NULL, N'0', CAST(N'2022-05-09T15:16:43.000' AS DateTime))
INSERT [dbo].[wms_stock] ([id], [locationid], [storedtypeid], [materialid], [productSN], [pencupSN], [optime]) VALUES (1, 53, 0, NULL, NULL, N'0', CAST(N'2022-05-09T15:16:56.000' AS DateTime))
INSERT [dbo].[wms_stock] ([id], [locationid], [storedtypeid], [materialid], [productSN], [pencupSN], [optime]) VALUES (1, 54, 0, NULL, NULL, N'0', CAST(N'2022-05-09T15:31:43.000' AS DateTime))
INSERT [dbo].[wms_stock] ([id], [locationid], [storedtypeid], [materialid], [productSN], [pencupSN], [optime]) VALUES (1, 55, 0, NULL, NULL, N'0', CAST(N'2022-05-09T15:31:57.000' AS DateTime))
INSERT [dbo].[wms_stock] ([id], [locationid], [storedtypeid], [materialid], [productSN], [pencupSN], [optime]) VALUES (1, 56, 0, NULL, NULL, N'0', CAST(N'2022-05-09T15:32:09.000' AS DateTime))
INSERT [dbo].[wms_stock] ([id], [locationid], [storedtypeid], [materialid], [productSN], [pencupSN], [optime]) VALUES (1, 57, 0, NULL, NULL, N'0', CAST(N'2022-05-09T15:32:25.000' AS DateTime))
INSERT [dbo].[wms_stock] ([id], [locationid], [storedtypeid], [materialid], [productSN], [pencupSN], [optime]) VALUES (1, 58, 0, NULL, NULL, N'0', CAST(N'2022-05-09T15:32:38.000' AS DateTime))
INSERT [dbo].[wms_stock] ([id], [locationid], [storedtypeid], [materialid], [productSN], [pencupSN], [optime]) VALUES (1, 59, 0, NULL, NULL, N'0', CAST(N'2022-05-09T15:32:52.000' AS DateTime))
INSERT [dbo].[wms_stock] ([id], [locationid], [storedtypeid], [materialid], [productSN], [pencupSN], [optime]) VALUES (1, 60, 0, NULL, NULL, N'0', CAST(N'2022-05-09T15:33:04.000' AS DateTime))
INSERT [dbo].[wms_stock] ([id], [locationid], [storedtypeid], [materialid], [productSN], [pencupSN], [optime]) VALUES (1, 61, 0, NULL, NULL, N'0', CAST(N'2022-05-09T15:33:16.000' AS DateTime))
INSERT [dbo].[wms_stock] ([id], [locationid], [storedtypeid], [materialid], [productSN], [pencupSN], [optime]) VALUES (1, 62, 0, NULL, NULL, N'0', CAST(N'2022-05-12T14:53:50.000' AS DateTime))
INSERT [dbo].[wms_stock] ([id], [locationid], [storedtypeid], [materialid], [productSN], [pencupSN], [optime]) VALUES (2, 1, 0, NULL, NULL, N'0', CAST(N'2022-05-09T15:33:39.000' AS DateTime))
INSERT [dbo].[wms_stock] ([id], [locationid], [storedtypeid], [materialid], [productSN], [pencupSN], [optime]) VALUES (2, 2, 0, NULL, NULL, N'0', CAST(N'2022-05-09T15:34:02.000' AS DateTime))
INSERT [dbo].[wms_stock] ([id], [locationid], [storedtypeid], [materialid], [productSN], [pencupSN], [optime]) VALUES (2, 3, 0, NULL, NULL, N'0', CAST(N'2022-05-09T15:34:15.000' AS DateTime))
INSERT [dbo].[wms_stock] ([id], [locationid], [storedtypeid], [materialid], [productSN], [pencupSN], [optime]) VALUES (2, 4, 0, NULL, NULL, N'0', CAST(N'2022-05-09T15:34:28.000' AS DateTime))
INSERT [dbo].[wms_stock] ([id], [locationid], [storedtypeid], [materialid], [productSN], [pencupSN], [optime]) VALUES (2, 5, 0, NULL, NULL, N'0', CAST(N'2022-05-09T15:34:40.000' AS DateTime))
INSERT [dbo].[wms_stock] ([id], [locationid], [storedtypeid], [materialid], [productSN], [pencupSN], [optime]) VALUES (2, 6, 0, NULL, NULL, N'0', CAST(N'2022-05-09T15:34:53.000' AS DateTime))
INSERT [dbo].[wms_stock] ([id], [locationid], [storedtypeid], [materialid], [productSN], [pencupSN], [optime]) VALUES (2, 7, 0, NULL, NULL, N'0', CAST(N'2022-05-09T15:37:23.000' AS DateTime))
INSERT [dbo].[wms_stock] ([id], [locationid], [storedtypeid], [materialid], [productSN], [pencupSN], [optime]) VALUES (2, 8, 0, NULL, NULL, N'0', CAST(N'2022-05-09T15:37:38.000' AS DateTime))
INSERT [dbo].[wms_stock] ([id], [locationid], [storedtypeid], [materialid], [productSN], [pencupSN], [optime]) VALUES (2, 9, 0, NULL, NULL, N'0', CAST(N'2022-05-09T15:37:57.000' AS DateTime))
INSERT [dbo].[wms_stock] ([id], [locationid], [storedtypeid], [materialid], [productSN], [pencupSN], [optime]) VALUES (2, 10, 0, NULL, NULL, N'0', CAST(N'2022-05-09T15:38:09.000' AS DateTime))
INSERT [dbo].[wms_stock] ([id], [locationid], [storedtypeid], [materialid], [productSN], [pencupSN], [optime]) VALUES (2, 11, 0, NULL, NULL, N'0', CAST(N'2022-05-09T15:38:22.000' AS DateTime))
INSERT [dbo].[wms_stock] ([id], [locationid], [storedtypeid], [materialid], [productSN], [pencupSN], [optime]) VALUES (2, 12, 0, NULL, NULL, N'0', CAST(N'2022-05-09T15:38:39.000' AS DateTime))
INSERT [dbo].[wms_stock] ([id], [locationid], [storedtypeid], [materialid], [productSN], [pencupSN], [optime]) VALUES (2, 13, 0, NULL, NULL, N'0', CAST(N'2022-05-09T15:38:49.000' AS DateTime))
INSERT [dbo].[wms_stock] ([id], [locationid], [storedtypeid], [materialid], [productSN], [pencupSN], [optime]) VALUES (2, 14, 0, NULL, NULL, N'0', CAST(N'2022-05-09T15:39:06.000' AS DateTime))
INSERT [dbo].[wms_stock] ([id], [locationid], [storedtypeid], [materialid], [productSN], [pencupSN], [optime]) VALUES (2, 15, 0, NULL, NULL, N'0', CAST(N'2022-05-09T15:39:22.000' AS DateTime))
INSERT [dbo].[wms_stock] ([id], [locationid], [storedtypeid], [materialid], [productSN], [pencupSN], [optime]) VALUES (2, 16, 0, NULL, NULL, N'0', CAST(N'2022-05-09T15:39:37.000' AS DateTime))
INSERT [dbo].[wms_stock] ([id], [locationid], [storedtypeid], [materialid], [productSN], [pencupSN], [optime]) VALUES (2, 17, 0, NULL, NULL, N'0', CAST(N'2022-05-09T15:39:49.000' AS DateTime))
INSERT [dbo].[wms_stock] ([id], [locationid], [storedtypeid], [materialid], [productSN], [pencupSN], [optime]) VALUES (2, 18, 0, NULL, NULL, N'0', CAST(N'2022-05-09T15:40:04.000' AS DateTime))
INSERT [dbo].[wms_stock] ([id], [locationid], [storedtypeid], [materialid], [productSN], [pencupSN], [optime]) VALUES (2, 19, 0, NULL, NULL, N'0', CAST(N'2022-05-09T15:40:17.000' AS DateTime))
INSERT [dbo].[wms_stock] ([id], [locationid], [storedtypeid], [materialid], [productSN], [pencupSN], [optime]) VALUES (2, 20, 0, NULL, NULL, N'0', CAST(N'2022-05-09T15:40:30.000' AS DateTime))
INSERT [dbo].[wms_stock] ([id], [locationid], [storedtypeid], [materialid], [productSN], [pencupSN], [optime]) VALUES (2, 21, 0, NULL, NULL, N'0', CAST(N'2022-05-09T15:40:42.000' AS DateTime))
INSERT [dbo].[wms_stock] ([id], [locationid], [storedtypeid], [materialid], [productSN], [pencupSN], [optime]) VALUES (2, 22, 0, NULL, NULL, N'0', CAST(N'2022-05-09T15:40:54.000' AS DateTime))
INSERT [dbo].[wms_stock] ([id], [locationid], [storedtypeid], [materialid], [productSN], [pencupSN], [optime]) VALUES (2, 23, 0, NULL, NULL, N'0', CAST(N'2022-05-09T15:41:06.000' AS DateTime))
INSERT [dbo].[wms_stock] ([id], [locationid], [storedtypeid], [materialid], [productSN], [pencupSN], [optime]) VALUES (2, 24, 0, NULL, NULL, N'0', CAST(N'2022-05-09T15:41:20.000' AS DateTime))
INSERT [dbo].[wms_stock] ([id], [locationid], [storedtypeid], [materialid], [productSN], [pencupSN], [optime]) VALUES (2, 25, 0, NULL, NULL, N'0', CAST(N'2022-05-09T15:41:37.000' AS DateTime))
INSERT [dbo].[wms_stock] ([id], [locationid], [storedtypeid], [materialid], [productSN], [pencupSN], [optime]) VALUES (2, 26, 0, NULL, NULL, N'0', CAST(N'2022-05-09T15:41:52.000' AS DateTime))
INSERT [dbo].[wms_stock] ([id], [locationid], [storedtypeid], [materialid], [productSN], [pencupSN], [optime]) VALUES (2, 27, 0, NULL, NULL, N'0', CAST(N'2022-05-09T15:42:09.000' AS DateTime))
INSERT [dbo].[wms_stock] ([id], [locationid], [storedtypeid], [materialid], [productSN], [pencupSN], [optime]) VALUES (2, 28, 0, NULL, NULL, N'0', CAST(N'2022-05-09T15:42:29.000' AS DateTime))
INSERT [dbo].[wms_stock] ([id], [locationid], [storedtypeid], [materialid], [productSN], [pencupSN], [optime]) VALUES (2, 29, 0, NULL, NULL, N'0', CAST(N'2022-05-09T15:42:47.000' AS DateTime))
INSERT [dbo].[wms_stock] ([id], [locationid], [storedtypeid], [materialid], [productSN], [pencupSN], [optime]) VALUES (2, 30, 0, NULL, NULL, N'0', CAST(N'2022-05-09T15:43:11.000' AS DateTime))
INSERT [dbo].[wms_stock] ([id], [locationid], [storedtypeid], [materialid], [productSN], [pencupSN], [optime]) VALUES (2, 31, 0, NULL, NULL, N'0', CAST(N'2022-05-09T15:43:23.000' AS DateTime))
INSERT [dbo].[wms_stock] ([id], [locationid], [storedtypeid], [materialid], [productSN], [pencupSN], [optime]) VALUES (2, 32, 0, NULL, NULL, N'0', CAST(N'2022-05-09T15:43:35.000' AS DateTime))
INSERT [dbo].[wms_stock] ([id], [locationid], [storedtypeid], [materialid], [productSN], [pencupSN], [optime]) VALUES (2, 33, 0, NULL, NULL, N'0', CAST(N'2022-05-09T15:43:54.000' AS DateTime))
INSERT [dbo].[wms_stock] ([id], [locationid], [storedtypeid], [materialid], [productSN], [pencupSN], [optime]) VALUES (2, 34, 0, NULL, NULL, N'0', CAST(N'2022-05-09T15:44:10.000' AS DateTime))
INSERT [dbo].[wms_stock] ([id], [locationid], [storedtypeid], [materialid], [productSN], [pencupSN], [optime]) VALUES (2, 35, 0, NULL, NULL, N'0', CAST(N'2022-05-09T15:44:22.000' AS DateTime))
INSERT [dbo].[wms_stock] ([id], [locationid], [storedtypeid], [materialid], [productSN], [pencupSN], [optime]) VALUES (2, 36, 0, NULL, NULL, N'0', CAST(N'2022-05-09T15:44:38.000' AS DateTime))
INSERT [dbo].[wms_stock] ([id], [locationid], [storedtypeid], [materialid], [productSN], [pencupSN], [optime]) VALUES (2, 37, 0, NULL, NULL, N'0', CAST(N'2022-05-09T15:44:51.000' AS DateTime))
INSERT [dbo].[wms_stock] ([id], [locationid], [storedtypeid], [materialid], [productSN], [pencupSN], [optime]) VALUES (2, 38, 0, NULL, NULL, N'0', CAST(N'2022-05-09T15:45:04.000' AS DateTime))
GO
INSERT [dbo].[wms_stock] ([id], [locationid], [storedtypeid], [materialid], [productSN], [pencupSN], [optime]) VALUES (2, 39, 0, NULL, NULL, N'0', CAST(N'2022-05-09T15:45:19.000' AS DateTime))
INSERT [dbo].[wms_stock] ([id], [locationid], [storedtypeid], [materialid], [productSN], [pencupSN], [optime]) VALUES (2, 40, 0, NULL, NULL, N'0', CAST(N'2022-05-09T15:45:35.000' AS DateTime))
INSERT [dbo].[wms_stock] ([id], [locationid], [storedtypeid], [materialid], [productSN], [pencupSN], [optime]) VALUES (2, 41, 0, NULL, NULL, N'0', CAST(N'2022-05-09T15:45:51.000' AS DateTime))
INSERT [dbo].[wms_stock] ([id], [locationid], [storedtypeid], [materialid], [productSN], [pencupSN], [optime]) VALUES (2, 42, 0, NULL, NULL, N'0', CAST(N'2022-05-09T15:46:07.000' AS DateTime))
INSERT [dbo].[wms_stock] ([id], [locationid], [storedtypeid], [materialid], [productSN], [pencupSN], [optime]) VALUES (2, 43, 0, NULL, NULL, N'0', CAST(N'2022-05-09T15:46:22.000' AS DateTime))
INSERT [dbo].[wms_stock] ([id], [locationid], [storedtypeid], [materialid], [productSN], [pencupSN], [optime]) VALUES (2, 44, 0, NULL, NULL, N'0', CAST(N'2022-05-09T15:46:36.000' AS DateTime))
INSERT [dbo].[wms_stock] ([id], [locationid], [storedtypeid], [materialid], [productSN], [pencupSN], [optime]) VALUES (2, 45, 0, NULL, NULL, N'0', CAST(N'2022-05-09T15:46:48.000' AS DateTime))
INSERT [dbo].[wms_stock] ([id], [locationid], [storedtypeid], [materialid], [productSN], [pencupSN], [optime]) VALUES (2, 46, 0, NULL, NULL, N'0', CAST(N'2022-05-09T15:47:01.000' AS DateTime))
INSERT [dbo].[wms_stock] ([id], [locationid], [storedtypeid], [materialid], [productSN], [pencupSN], [optime]) VALUES (2, 47, 0, NULL, NULL, N'0', CAST(N'2022-05-09T15:47:13.000' AS DateTime))
INSERT [dbo].[wms_stock] ([id], [locationid], [storedtypeid], [materialid], [productSN], [pencupSN], [optime]) VALUES (2, 48, 0, NULL, NULL, N'0', CAST(N'2022-05-09T15:47:27.000' AS DateTime))
INSERT [dbo].[wms_stock] ([id], [locationid], [storedtypeid], [materialid], [productSN], [pencupSN], [optime]) VALUES (2, 49, 0, NULL, NULL, N'0', CAST(N'2022-05-09T15:47:41.000' AS DateTime))
INSERT [dbo].[wms_stock] ([id], [locationid], [storedtypeid], [materialid], [productSN], [pencupSN], [optime]) VALUES (2, 50, 0, NULL, NULL, N'0', CAST(N'2022-05-09T15:47:58.000' AS DateTime))
INSERT [dbo].[wms_stock] ([id], [locationid], [storedtypeid], [materialid], [productSN], [pencupSN], [optime]) VALUES (2, 51, 0, NULL, NULL, N'0', CAST(N'2022-05-09T15:48:14.000' AS DateTime))
INSERT [dbo].[wms_stock] ([id], [locationid], [storedtypeid], [materialid], [productSN], [pencupSN], [optime]) VALUES (2, 52, 0, NULL, NULL, N'0', CAST(N'2022-05-09T15:48:27.000' AS DateTime))
INSERT [dbo].[wms_stock] ([id], [locationid], [storedtypeid], [materialid], [productSN], [pencupSN], [optime]) VALUES (2, 53, 0, NULL, NULL, N'0', CAST(N'2022-05-09T15:48:48.000' AS DateTime))
INSERT [dbo].[wms_stock] ([id], [locationid], [storedtypeid], [materialid], [productSN], [pencupSN], [optime]) VALUES (2, 54, 0, NULL, NULL, N'0', CAST(N'2022-05-09T15:49:01.000' AS DateTime))
INSERT [dbo].[wms_stock] ([id], [locationid], [storedtypeid], [materialid], [productSN], [pencupSN], [optime]) VALUES (2, 55, 0, NULL, NULL, N'0', CAST(N'2022-05-09T15:49:17.000' AS DateTime))
INSERT [dbo].[wms_stock] ([id], [locationid], [storedtypeid], [materialid], [productSN], [pencupSN], [optime]) VALUES (2, 56, 0, NULL, NULL, N'0', CAST(N'2022-05-09T15:49:32.000' AS DateTime))
INSERT [dbo].[wms_stock] ([id], [locationid], [storedtypeid], [materialid], [productSN], [pencupSN], [optime]) VALUES (2, 57, 0, NULL, NULL, N'0', CAST(N'2022-05-09T15:49:43.000' AS DateTime))
INSERT [dbo].[wms_stock] ([id], [locationid], [storedtypeid], [materialid], [productSN], [pencupSN], [optime]) VALUES (2, 58, 0, NULL, NULL, N'0', CAST(N'2022-05-09T15:51:35.000' AS DateTime))
INSERT [dbo].[wms_stock] ([id], [locationid], [storedtypeid], [materialid], [productSN], [pencupSN], [optime]) VALUES (2, 59, 0, NULL, NULL, N'0', CAST(N'2022-05-09T15:51:52.000' AS DateTime))
INSERT [dbo].[wms_stock] ([id], [locationid], [storedtypeid], [materialid], [productSN], [pencupSN], [optime]) VALUES (2, 60, 0, NULL, NULL, N'0', CAST(N'2022-05-09T15:52:18.000' AS DateTime))
INSERT [dbo].[wms_stock] ([id], [locationid], [storedtypeid], [materialid], [productSN], [pencupSN], [optime]) VALUES (2, 61, 0, NULL, NULL, N'0', CAST(N'2022-05-09T15:52:32.000' AS DateTime))
INSERT [dbo].[wms_stock] ([id], [locationid], [storedtypeid], [materialid], [productSN], [pencupSN], [optime]) VALUES (2, 62, 0, NULL, NULL, N'0', CAST(N'2022-05-12T14:54:11.000' AS DateTime))
GO
ALTER TABLE [dbo].[bas_product]  WITH CHECK ADD  CONSTRAINT [FK_bas_product_bas_product] FOREIGN KEY([id])
REFERENCES [dbo].[bas_product] ([id])
GO
ALTER TABLE [dbo].[bas_product] CHECK CONSTRAINT [FK_bas_product_bas_product]
GO
ALTER TABLE [dbo].[sch_order]  WITH CHECK ADD  CONSTRAINT [FK_sch_order_bas_product] FOREIGN KEY([productid])
REFERENCES [dbo].[bas_product] ([id])
GO
ALTER TABLE [dbo].[sch_order] CHECK CONSTRAINT [FK_sch_order_bas_product]
GO
USE [master]
GO
ALTER DATABASE [ChongQingGZYMESDB] SET  READ_WRITE 
GO
