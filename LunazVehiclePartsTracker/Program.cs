using LunazVehiclePartsTracker.Data;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Configuration;
using Syncfusion.Blazor;
using Syncfusion.Blazor.Popups;
using Blazor3D;
using Microsoft.AspNetCore.StaticFiles;
using SAPB1Commons.ServiceLayer.Models.SAPB1;
using SL = SAPB1Commons.ServiceLayer;
using Microsoft.Extensions.Configuration;
using SAPB1Commons.ServiceLayer;

var builder = WebApplication.CreateBuilder(args);

var startup = new LunazVehiclePartsTracker.Startup(builder);

startup.ConfigureServices();

startup.Configure().Run();
