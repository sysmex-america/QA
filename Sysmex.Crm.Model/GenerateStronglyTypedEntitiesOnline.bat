@echo off
title Generating model...

..\Tools\CrmSvcUtil\CrmSvcUtil ^
/codewriterfilter:"SonomaPartners.Xrm.ModelGen.FilteringService, SonomaPartners.Xrm.ModelGen" ^
/namingservice:"SonomaPartners.Xrm.ModelGen.NamingService, SonomaPartners.Xrm.ModelGen" ^
/out:Sysmex.cs ^
/serviceContextName:SysmexContext ^
/namespace:Sysmex.Crm.Model ^
/modelfile:Entities.xml ^
/generateActions ^
/interactivelogin

pause
