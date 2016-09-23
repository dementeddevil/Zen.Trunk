#pragma once
#include "InstallerBase.h"

class CPrincipalInstaller : public CInstallerBase
{
public:
	CPrincipalInstaller (MSIHANDLE hInstall);

	CString GetDomainOrControllerName (LPCTSTR pszDomain);
};