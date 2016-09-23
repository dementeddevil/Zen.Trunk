#include "stdafx.h"
#include "ServiceInstaller.h"

extern "C" UINT __stdcall ValidateInstanceName (MSIHANDLE hInstall)
{
	CServiceInstaller tHelper (hInstall);
	try
	{
		tHelper.ValidateInstanceName ();
		return ERROR_SUCCESS;
	}
	catch (const _com_error& e)
	{
		tHelper.LogError (_T("Failed with error %08lx"), e.Error ());
		return e.WCode ();
	}
}

extern "C" UINT __stdcall ValidateServiceCredentials (MSIHANDLE hInstall)
{
	CServiceInstaller tHelper (hInstall);
	try
	{
		tHelper.ValidateServiceCredentials ();
		return ERROR_SUCCESS;
	}
	catch (const _com_error& e)
	{
		tHelper.LogError (_T("Failed with error %08lx"), e.Error ());
		return e.WCode ();
	}
}

extern "C" UINT __stdcall ValidateDomainServiceCredentials (MSIHANDLE hInstall)
{
	CServiceInstaller tHelper (hInstall);
	try
	{
		try
		{
			tHelper.LookupAccountName ();
		}
		catch (const _com_error& e)
		{
			tHelper.LogInfo (_T("Failed to lookup service credentials. [HR=%08lX]"), e.Error ());
			return ERROR_SUCCESS;
		}
		tHelper.SetProperty (_T("SUI_SERVICEACCOUNT_VALID"), _T("1"));
		return ERROR_SUCCESS;
	}
	catch (const _com_error& e)
	{
		tHelper.LogError (_T("Failed with error. [HR=%08lX]"), e.Error ());
		return e.WCode ();
	}
}

// This CA is run from the outer installer in order to determine the name
//	of the appropriate transform to apply to the inner installer.
//	the CA functions should be split into seperate DLLs but why bother!
extern "C" UINT __stdcall UpdateFreeServiceInstance (MSIHANDLE hInstall)
{
	CServiceInstaller tHelper (hInstall);
	try
	{
		tHelper.UpdateFreeServiceInstance ();
		return ERROR_SUCCESS;
	}
	catch (const _com_error& e)
	{
		tHelper.LogError (_T("Failed with error %08lx"), e.Error ());
		return e.WCode ();
	}
}

CServiceInstaller::CServiceInstaller (MSIHANDLE hInstall)
	: CInstallerBase (hInstall)
{
}

void CServiceInstaller::ValidateInstanceName ()
{
	// First of all we need to retrieve the instance type
	CString strInstanceType, strInstanceName;
	strInstanceType = GetProperty (_T("INSTANCETYPE"));
	if (strInstanceType == _T("0"))
	{
		SetProperty (_T("SUI_INSTANCENAME_VALID"), _T("1"));
	}
	else
	{
		strInstanceName = GetProperty (_T("INSTANCENAME"));

		// Instance name cannot be empty or contain reserved characters.
		if (strInstanceName.GetLength () == 0)
		{
			LogInfoMessage (_T("Custom instance name cannot be empty."));
			throw _com_error (ERROR_INSTALL_FAILURE);
		}
		else if (strInstanceName.Find (_T(".$/\\[]")) != -1)
		{
			LogInfoMessage (_T("Custom instance name contains invalid characters."));
			throw _com_error (ERROR_INSTALL_FAILURE);
		}

		// Check service name length is within allowed limits
		CString strServiceName, strServiceNamePrefix;
		strServiceNamePrefix = GetProperty (_T("InstancePrefix"));
		strServiceName.Format (_T("%s$%s.XX"), strServiceNamePrefix, strInstanceName);
		if (strServiceName.GetLength () > 256)
		{
			LogInfo (
				_T("Resultant service name [%s] too long (256 chars max)."),
				strServiceName);
			throw _com_error (ERROR_INSTALL_FAILURE);
		}

		SetProperty (_T("SUI_INSTANCENAME_VALID"), _T("1"));
	}
}

void CServiceInstaller::ValidateServiceCredentials ()
{
	// First of all we need to retrieve the service account type
	// NOTE: We default to Local Service if unknown.
	CString strAccountType;
	strAccountType = GetProperty (_T("SERVICEACCOUNTTYPE"));
	if (strAccountType.GetLength () == 0)
	{
		strAccountType = _T("1");
		SetProperty (_T("SERVICEACCOUNTTYPE"), strAccountType);
	}

	// Setup the service account/domain and credentials.
	int nAccountType = _ttoi (strAccountType);
	switch (nAccountType)
	{
	case 1:		// Local Service
		SetProperty (_T("SERVICEFQACCOUNT"), _T("NT AUTHORITY\\LocalService"));
		SetProperty (_T("SERVICEACCOUNT"), _T("LocalService"));
		SetProperty (_T("SERVICEDOMAIN"), _T("NT AUTHORITY"));
		SetProperty (_T("SERVICEPASSWORD"), _T(""));
		break;
	case 2:		// Network Service
		SetProperty (_T("SERVICEFQACCOUNT"), _T("NT AUTHORITY\\NetworkService"));
		SetProperty (_T("SERVICEACCOUNT"), _T("NetworkService"));
		SetProperty (_T("SERVICEDOMAIN"), _T("NT AUTHORITY"));
		SetProperty (_T("SERVICEPASSWORD"), _T(""));
		break;
	case 3:		// Custom Account
		try
		{
			LookupAccountName ();
		}
		catch (const _com_error& e)
		{
			LogInfo(
				_T("Failed to lookup service credentials. [HR=%08lX]"),
				e.Error());
			throw;
		}
		break;
	default:	// Local System
		SetProperty (_T("SERVICEFQACCOUNT"), _T(""));
		SetProperty (_T("SERVICEACCOUNT"), _T("SYSTEM"));
		SetProperty (_T("SERVICEDOMAIN"), _T("NT AUTHORITY"));
		SetProperty (_T("SERVICEPASSWORD"), _T(""));
		break;
	}
	SetProperty (_T("SUI_SERVICEACCOUNT_VALID"), _T("1"));
}

void CServiceInstaller::UpdateFreeServiceInstance ()
{
	CString strInstancePrefix, strMaxInstanceCount;

	// Get the service instance name prefix
	strInstancePrefix = GetProperty (_T("InstancePrefix"));

	// Get maximum instance count (default is 32)
	strMaxInstanceCount = GetProperty (_T("MaxInstanceCount"));
	int maxInstanceCount = 32;
	if (strMaxInstanceCount.GetLength () > 0)
	{
		maxInstanceCount = _ttoi (strMaxInstanceCount);
	}

	// Find the next free "<InstancePrefix>$<Instance>.<InstanceIndex>"
	//	service name combination
	int freeInstanceIndex = GetFreeServiceInstance (
		strInstancePrefix, maxInstanceCount);
	if (freeInstanceIndex == 0)
	{
		LogInfoMessage (_T("Failed to determine free instance index."));
		throw _com_error (ERROR_INSTALL_FAILURE);
	}

	// Update the FREEINSTANCEINDEX property
	CString strFreeInstance, strTransformName;
	strFreeInstance.Format (_T("%d"), freeInstanceIndex);
	SetProperty (_T("FREEINSTANCEINDEX"), strFreeInstance);

	// Update the FREEINSTANCETRANSFORM property
	strTransformName.Format (_T("InstanceTransform%d.mst"), freeInstanceIndex);
	SetProperty (_T("FREEINSTANCETRANSFORM"), strTransformName);
}

int CServiceInstaller::GetFreeServiceInstance (LPCTSTR pszServicePrefix, int maxInstanceCount)
{
	CServiceInstanceMap serviceMap;
	PopulateServiceInstance (pszServicePrefix, maxInstanceCount, serviceMap);
	for (int index = 1; index <= maxInstanceCount; ++index)
	{
		if (serviceMap.find (index) == serviceMap.end ())
		{
			// Not found so return now
			return index;
		}
	}
	return 0;
}

void CServiceInstaller::PopulateServiceInstance (LPCTSTR pszServicePrefix, 
	 int maxInstanceCount, CServiceInstanceMap& serviceMap)
{
	SC_HANDLE scm = NULL;
	LPBYTE pBlock = NULL;
	try
	{
		// Open service control manager
		scm = OpenSCManager (NULL, NULL, SC_MANAGER_ENUMERATE_SERVICE);
		if (scm == NULL)
		{
			return;
		}

		// Establish the service name prefix we are searching for
		CString strServicePrefix;
		strServicePrefix.Format (_T("%s$"), pszServicePrefix);

		// Allocate temporary storage
		pBlock = (LPBYTE) malloc (16384);

		// Loop through the service information blocks
		DWORD dwResumeHandle = 0, dwServicesReturned = 0, dwBytesNeeded = 0;
		bool bContinue = true;
		while (bContinue)
		{
			// Get the next block of service information
			int result = EnumServicesStatusEx (scm, SC_ENUM_PROCESS_INFO, SERVICE_WIN32,
				SERVICE_STATE_ALL, pBlock, 16384, &dwBytesNeeded, &dwServicesReturned, 
				&dwResumeHandle, NULL);
			if (result == 0)
			{
				int error = GetLastError ();
				if (error != ERROR_MORE_DATA)
				{
					break;
				}
			}
			else
			{
				bContinue = false;
			}

			// Walk list of services returned
			LPENUM_SERVICE_STATUS pStatus = (LPENUM_SERVICE_STATUS) pBlock;
			for (DWORD dwIndex = 0; dwIndex < dwServicesReturned; ++dwIndex)
			{
				// Does this service name have a matching prefix
				CString strServiceName (pStatus[dwIndex].lpServiceName);
				if (strServiceName.Find (strServicePrefix) == 0)
				{
					// Determine the instance number
					int instanceIndex = strServiceName.Find (_T('.'));
					if (instanceIndex != -1)
					{
						int instanceNameIndex = strServicePrefix.GetLength ();
						if (instanceIndex > instanceNameIndex)
						{
							int instance = _ttoi (strServiceName.Mid (instanceIndex + 1));
							if (instance >= 1 && instance <= maxInstanceCount)
							{
								int instanceLength = 
									instanceIndex - instanceNameIndex;
								CString strInstance = strServiceName.Mid (
									instanceNameIndex, instanceLength);

								if (serviceMap.find (instance) != serviceMap.end ())
								{
									// CRITICAL ERROR: Multiple instance names 
									//	with same instance index detected!
								}
								else
								{
									serviceMap.insert (CServiceInstanceMapValue(
										instance, strInstance));
								}
							}
						}
					}
				}
			}
		}
	}
	catch(...)
	{
		if (pBlock != NULL)
		{
			free (pBlock);
		}
		CloseServiceHandle (scm);
		throw;
	}
	if (pBlock != NULL)
	{
		free (pBlock);
	}
	CloseServiceHandle (scm);
}

void CServiceInstaller::LookupAccountName ()
{
	// Check account properties have been set
	CString strAccount, strPassword, strDomain;
	strAccount = GetProperty (_T("SERVICEACCOUNT"));
	if (strAccount.GetLength () == 0)
	{
		throw _com_error (ERROR_INSTALL_FAILURE);
	}

	// Use any included domain name in account name if specified
	//	otherwise use the service domain property.
	int slashIndex = strAccount.Find (_T('\\'));
	if (slashIndex != -1)
	{
		// Domain specified in account name
		strDomain = strAccount.Mid (0, slashIndex);
		strAccount = strAccount.Mid (slashIndex + 1);
	}
	else
	{
		strDomain = GetProperty (_T("SERVICEDOMAIN"));
	}

	// Use local machine name if domain is empty or localhost prefix
	if (strDomain.GetLength () == 0 || strDomain == _T("."))
	{
		DWORD dwSize = MAX_COMPUTERNAME_LENGTH + 1;
		GetComputerNameEx (ComputerNameNetBIOS, strDomain.GetBuffer (dwSize), &dwSize);
		strDomain.ReleaseBuffer (dwSize);
	}

	// Lookup the account SID
	CString strFQAccount;
	strFQAccount.Format (_T("%s\\%s"), strDomain, strAccount);
	CSid sid;
	if (!sid.LoadAccount (strFQAccount))
	{
		throw _com_error (ERROR_INSTALL_FAILURE);
	}

	// If we get this far then setup new account name
	SetProperty (_T("SERVICEFQACCOUNT"), strFQAccount);
	SetProperty (_T("SERVICEACCOUNT"), strAccount);
	SetProperty (_T("SERVICEDOMAIN"), strDomain);
}

bool CServiceInstaller::InitLsaString(PLSA_UNICODE_STRING pLsaString,
								   LPCWSTR pwszString)
{
	// Sanity checks
	if (pLsaString == NULL)
	{
		return false;
	}
	DWORD dwLen = 0;
	if (pwszString != NULL) 
	{
		dwLen = (DWORD) wcslen (pwszString);
		if (dwLen > 0x7ffe)   // String is too large
		{
			return false;
		}
	}

	// Store the string.
	USES_CONVERSION;
	pLsaString->Buffer = (WCHAR*) pwszString;
	pLsaString->Length = (USHORT) dwLen * sizeof (WCHAR);
	pLsaString->MaximumLength = (USHORT)(dwLen + 1) * sizeof (WCHAR);
	return true;
}
