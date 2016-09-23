#pragma once
#include "InstallerBase.h"

class CServiceInstaller : public CInstallerBase
{
public:
	typedef std::map<int, CString> CServiceInstanceMap;
	typedef CServiceInstanceMap::value_type CServiceInstanceMapValue;
	typedef CServiceInstanceMap::iterator CServiceInstanceMapIter;
	typedef CServiceInstanceMap::const_iterator CServiceInstanceMapConstIter;

	CServiceInstaller (MSIHANDLE hInstall);

	void ValidateInstanceName ();
	void ValidateServiceCredentials ();
	void UpdateFreeServiceInstance ();
	int GetFreeServiceInstance (LPCTSTR pszServicePrefix, int maxInstanceCount);

	void LookupAccountName ();

private:
	void PopulateServiceInstance (LPCTSTR pszServicePrefix, 
		 int maxInstanceCount, CServiceInstanceMap& serviceMap);

	bool InitLsaString(LSA_UNICODE_STRING *pLsaString, LPCWSTR pwszString);
};
