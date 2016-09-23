#include "stdafx.h"
#include "PrincipalInstaller.h"

CPrincipalInstaller::CPrincipalInstaller (MSIHANDLE hInstall)
: CInstallerBase (hInstall)
{
}

CString CPrincipalInstaller::GetDomainOrControllerName (LPCTSTR pszDomain)
{
	USES_CONVERSION;
	CString strDomainOrController;
	if (pszDomain != NULL && _tcslen (pszDomain) > 0)
	{
		PDOMAIN_CONTROLLER_INFOW pDomainControllerInfo = NULL;
		UINT er = DsGetDcName (NULL, T2CW (pszDomain), NULL, NULL, NULL, &pDomainControllerInfo);
		if (ERROR_SUCCESS == er)
		{
			strDomainOrController = pDomainControllerInfo->DomainControllerName + 2;  //Add 2 so that we don't get the \\ prefix
		}
		else
		{
			strDomainOrController = pszDomain;
		}
		if (pDomainControllerInfo != NULL)
		{
			::NetApiBufferFree(static_cast<LPVOID>(pDomainControllerInfo));
		}
	}
	return strDomainOrController;
}
