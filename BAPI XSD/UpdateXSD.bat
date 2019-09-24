"C:\Program Files (x86)\Microsoft SDKs\Windows\v10.0A\bin\NETFX 4.6.2 Tools\xsd.exe" Types.xsd Rfc.xsd /classes /namespace:Sysmex.Crm.Plugins.Common /outputdir:..\Sysmex.Crm.Plugins\Common
move /y "..\Sysmex.Crm.Plugins\Common\Types_Rfc.cs" "..\Sysmex.Crm.Plugins\Common\Rfc.cs"
pause