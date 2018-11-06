SELECT distinct

 

dbo.[File].IDFile AS 'FILENR',

dbo.[File].ProductDescription AS 'PACKAGE',

'Claim month' =

case

       when month(dbo.[File].InsertDate) = 2 then 'February'

       else 'Доделать !!!'

end,

'IMMOBILISING EVENT' =

case

       when dbo.[Event].Event = 'поломка X1' then 'BREAKDOWN'

       when dbo.[Event].Event = 'ошибка пилота X1' then 'PILOT ERROR'

       else 'INFO FILE'

end,

'ACTION TAKEN' =

case

       when dbo.DossierClosureReason.[Description] = 'Cancelled call out' then 'CANCELLED CALL OUT'

       when dbo.DossierClosureReason.[Description] = 'Succesfull repair on' then 'SUCCESSFUL REPAIR ON THE SPOT'

       when dbo.DossierClosureReason.[Description] = 'Towed to near dealer' then 'TOWED TO DEALER'

       when dbo.DossierClosureReason.[Description] = 'Other' then 'OTHER'

       else 'INFO FILE'

end,

 

dbo.[File].EventCountry AS 'COUNTRY OF IMMOBILISATION',

dbo.[File].Province AS 'REGION OF IMMOBILISATION',

dbo.[File].EventPlace AS 'CITY OF IMMOBILISATION',

Garage AS 'REPAIRING DEALER',

Dealer AS 'SELLING DEALER',

dbo.[File].LicencePlate AS 'REG. NO',

dbo.Models.Model AS 'MODEL',

--month(dbo.[File].InsertDate) as 'Claim month',

convert (varchar,RegistrationDate,104) AS 'FIRST_REGISTRATION_DATE',

convert(varchar,dbo.[File].InsertDate,104) AS 'ASSISTANCE_DATE',

 

 

/* Ебаторий

'Rep.Dealer' =

       case

             when ActionTaken = 'TOWED TO DEALER' then Garage

             else '2'

       end,

       */

BenefitDescription AS 'Benefit',

--dbo.DossierClosureReason.[Description] AS 'STATUS',

 

 

 

GarageDealerCode AS 'REPAIRING_DEALER_CODE',

Garage AS 'REPAIRING_DEALER_NAME',

 

 

 

ChassisNumber AS 'CHASSIS_NR',

dbo.Damage.IsmdDamage AS 'FAULT_CODE',

dbo.Expenditure.InterventionDateTime AS 'INTERVENTION_DELAY_IN_MINUTES',

dbo.EndCustomer.Surname + ' ' + dbo.EndCustomer.[Name] AS 'SUBSCRIBER',

dbo.[File].ContractDescription AS 'POLICY_NR',

convert (varchar,StartDate,20) AS 'POLICY_START_DATE',

dbo.Benefit.IDBenefit AS 'IDBenefit',

dbo.Expenditure.IDExpenditure,

dbo.[File].ProductDescription AS 'CONTRACT_CODE',

'Russian Federation' AS 'COUNTRY_OF_THE_CONTRACT'

FROM dbo.[File]

 

left join dbo.[FileT] on dbo.[File].IDFile = dbo.[FileT].IDFile

left join dbo.Expenditure on dbo.[File].IDFile=dbo.Expenditure.IDFile

left join dbo.Benefit on dbo.Expenditure.IDBenefit=dbo.Benefit.IDBenefit

left join dbo.Models on dbo.FileT.IDModel=dbo.Models.IDModel

left join dbo.Damage on dbo.FileT.IDDamage = dbo.Damage.IDDamage

left join dbo.DamageTranslate on dbo.Damage.IsmdDamage=dbo.DamageTranslate.Code

left join dbo.ExpenditureDetail1 on dbo.Expenditure.IDExpenditure=dbo.ExpenditureDetail1.IDExpenditure

left join dbo.EndCustomer on dbo.[File].IDEndCustomer=dbo.EndCustomer.IDEndCustomer

left join dbo.DossierClosureReason on dbo.[File].ClosureReasonCode=dbo.DossierClosureReason.Code

left join dbo.ProductWarranty ON dbo.Expenditure.[IDProductWarranty]=dbo.ProductWarranty.[IDProductWarranty]

LEFT JOIN dbo.Warranty ON dbo.ProductWarranty.IDWarranty=dbo.Warranty.IDWarranty

LEFT JOIN dbo.WarrantyCategory ON dbo.Warranty.IDWarrantyCategory=dbo.WarrantyCategory.IDWarrantyCategory

left join dbo.[Chrysler trans] ON (dbo.Benefit.IDBenefit=dbo.[Chrysler trans].Benefit)

  AND ((dbo.WarrantyCategory.IDWarrantyCategory=dbo.[Chrysler trans].[Warranty category])

        OR (dbo.[Chrysler trans].[Warranty category] IS NULL))

left join dbo.[Event] ON dbo.[File].IDEvent=dbo.[Event].IDEvent

left join dbo.Supplier ON dbo.Expenditure.IDSupplier=dbo.Supplier.IDSupplier

Left JOIN

(SELECT dbo.HQStatistics.DossierID,dbo.HQStatistics.CustomerImmobilizationDist,dbo.HQStatistics.ImmobilizationRepDealerDist

FROM dbo.HQStatistics INNER JOIN

(SELECT DossierID,Max(Timestamp) AS 'TimeSt' FROM dbo.HQStatistics

GROUP BY DossierID) AS T1

ON (dbo.HQStatistics.DossierID=T1.DossierID AND dbo.HQStatistics.[Timestamp]=T1.TimeSt)) AS T2

ON dbo.[FileT].IDFile=T2.DossierID

LEFT JOIN dbo.PSACodesPeu AS PC ON (dbo.Expenditure.IDBenefit=PC.IDBenefit AND dbo.Expenditure.IDActionTaken=PC.IDActionTaken)

 

WHERE --(dbo.[File].IDFile like 'C5%')

    --or(dbo.[File].IDFile like 'X1%')

    --or(dbo.[File].IDFile like 'M3%')

    --and

    year([dbo].[File].[InsertDate])=2017 and month([dbo].[File].[InsertDate])=02  AND dbo.[File].ProductDescription like 'Suzuki%' and dbo.[File].IDFile not like 'QC%' and dbo.Expenditure.[Status] <> 4

ORDER BY FILENR