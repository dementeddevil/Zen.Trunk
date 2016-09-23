#include "stdafx.h"
#include "UrlReservationInstaller.h"

LPCTSTR vActionableUrlReservationQuery = _T("SELECT `Reservation`, `Component_`, `Url`, `Attributes` FROM `UrlReservation`");
enum eActionableUrlReservationQuery { vurqReservation = 1, vurqComponent, vurqUrl, vurqAttributes };

LPCTSTR vActionableUrlReservationAclQuery = _T("SELECT `Acl`, `Reservation_`, `Name`, `Domain`, `Attributes` FROM `UrlReservationAcl` WHERE `Reservation_`=?");
enum eActionableUrlReservationAclQuery { vuraqAcl = 1, vuraqReservation, vuraqName, vuraqDomain, vuraqAttributes };

extern "C" UINT __stdcall SchedUrlReservationsInstall (MSIHANDLE hInstall)
{
	CUrlReservationInstaller tHelper (hInstall);
	try
	{
		tHelper.ScheduleUrlReservations (WCA_TODO_INSTALL);
		return 0;
	}
	catch (const _com_error& e)
	{
		tHelper.LogError (_T("Failed with error %08lx"), e.Error ());
		return e.WCode ();
	}
}

extern "C" UINT __stdcall SchedUrlReservationsUninstall (MSIHANDLE hInstall)
{
	CUrlReservationInstaller tHelper (hInstall);
	try
	{
		tHelper.ScheduleUrlReservations (WCA_TODO_UNINSTALL);
		return 0;
	}
	catch (const _com_error& e)
	{
		tHelper.LogError (_T("Failed with error %08lx"), e.Error ());
		return e.WCode ();
	}
}

extern "C" UINT __stdcall ExecUrlReservation(MSIHANDLE hInstall)
{
	CUrlReservationInstaller tHelper (hInstall);
	try
	{
		tHelper.ExecuteUrlReservation();
	}
	catch(const _com_error& error)
	{
		tHelper.LogError (_T("Caught exception. [HR=%08lX]"), error.Error ());
		return ERROR_INSTALL_FAILURE;
	}
	return ERROR_SUCCESS;
}

CUrlReservationInstaller::CUrlReservationInstaller (MSIHANDLE hInstall)
	: CInstallerBase (hInstall)
{
}

void CUrlReservationInstaller::ScheduleUrlReservations (WCA_TODO todoScheduled)
{
	try
	{
		// Check we actually have a table...
		if (!GetActiveDatabase().IsTable (_T("UrlReservation")))
		{
			return;
		}

		LogInfo (_T("ScheduleUrlReservations - Pending Open/Exec View"));
		CMsiView view (GetActiveDatabase().OpenExecuteView (vActionableUrlReservationQuery));

		CString strAllCAData;
		int iInstanceCount = 0;

		LogInfo (_T("ScheduleUrlReservations - Pending Fetch Loop"));
		CMsiRecord record;
		while (view.Fetch (record))
		{
			LogInfo (_T("ScheduleUrlReservations - Pending Component Check"));
			CString strComponent = record.GetString (vurqComponent);
			WCA_TODO todoComponent = GetComponentToDo (strComponent);
			if ((todoComponent == WCA_TODO_REINSTALL ? WCA_TODO_INSTALL : todoComponent) != todoScheduled)
			{
				LogInfo (_T("Component '%s' action state (%d) doesn't match request (%d)"),
					strComponent, todoComponent, todoScheduled);
				continue;
			}

			// Read remaining data from table row
			LogInfo (_T("ScheduleUrlReservations - Pending Reservation Read"));
			CString strKey, strUrl, strAcl, strResult;
			int iAttributes;
			strKey = record.GetString (vurqReservation);
			strUrl = GetRecordFormattedString (record, vurqUrl);
			iAttributes = record.GetInteger (vurqAttributes);
			
			// Fetch ACL data for this reservation
			LogInfo (_T("ScheduleUrlReservations - Pending Open/Exec Acl View"));
			CMsiRecord urlParam (1);
			urlParam.SetString (1, strKey);
			CMsiView aclView (GetActiveDatabase().OpenExecuteView (vActionableUrlReservationAclQuery, urlParam));
			CMsiRecord aclRecord;
			while (aclView.Fetch (aclRecord))
			{
				CString strAclKey, strAclUser, strAclDomain;
				int iAclAttributes;

				LogInfo (_T("ScheduleUrlReservations - Pending Url Reservation Acl Read"));
				strAclKey = aclRecord.GetString (vuraqAcl);
				strAclUser = GetRecordFormattedString (aclRecord, vuraqName);
				strAclDomain = GetRecordFormattedString (aclRecord, vuraqDomain);
				iAclAttributes = aclRecord.GetInteger (vuraqAttributes);

				// Build CA data block
				strResult.Format (_T("%s|%s|%d"), 
					strAclUser, strAclDomain, iAclAttributes);

				// Append to ACL information
				if (strAcl.IsEmpty())
				{
					strAcl = strResult;
				}
				else
				{
					strAcl.AppendFormat (_T("|%s"), strResult);
				}
			}

			if (strAcl.IsEmpty())
			{
				// Don't bother writing reservations with empty DACLs
				continue;
			}

			// Build CA data for this entry
			strResult.Format (_T("%d\t%s\t%d\t%s"), 
				todoComponent, (LPCTSTR) strUrl, iAttributes, (LPCTSTR) strAcl);

			// Merge CA data and update instance count.
			if (strAllCAData.IsEmpty ())
			{
				strAllCAData = strResult;
			}
			else
			{
				strAllCAData.AppendFormat (_T("\t%s"), strResult);
			}
			++iInstanceCount;
		}

		// If we have CA data then schedule actions
		if (!strAllCAData.IsEmpty ())
		{
			if (todoScheduled == WCA_TODO_INSTALL)
			{
				DoDeferredAction (_T("SuiExecUrlReservationsInstall"), strAllCAData, iInstanceCount * COST_URLRESERVATION_ADD); 
				DoDeferredAction (_T("SuiRollbackUrlReservationsInstall"), strAllCAData, iInstanceCount * COST_URLRESERVATION_ADD); 
			}
			else
			{
				DoDeferredAction (_T("SuiExecUrlReservationsUninstall"), strAllCAData, iInstanceCount * COST_URLRESERVATION_DELETE); 
				DoDeferredAction (_T("SuiRollbackUrlReservationsUninstall"), strAllCAData, iInstanceCount * COST_URLRESERVATION_DELETE); 
			}
		}
	}
	catch (const _com_error& e)
	{
		LogError (_T("Exception caught while reading url reservations: %08lX"),
			e.Error ());
		throw e;
	}
}

/*void CUrlReservationInstaller::DeferredExecuteUrlReservationActions (CUrlReservation* pReservation)
{
	if (pReservation != NULL)
	{
		for (CUrlReservation* pIter = pReservation; pIter != NULL; pIter = pIter->m_pNext)
		{
			LogInfo (_T("ExecuteUrlReservationActions - Pending Fetch CB Data"));
			CString strCAData = pIter->GetCBData ();

			// Check for reservation existence
			LogInfo (_T("ExecuteUrlReservationActions - Pending Url Reservation Exists Check"));
			bool bExists = false;
			try
			{
				bExists = pIter->GetUrlReservationExists ();
			}
			catch (const _com_error& e)
			{
				LogError (_T("Failed to check url reservation exists (%s). [%08lX]"), 
					pIter->m_strUrl, 
					e.Error ());
			}
			LogInfo (_T("Url Reservation (%s) %s."), pIter->m_strUrl, bExists ? _T("exists") : _T("does not exist"));
			if (pIter->m_todoComponent == WCA_TODO_INSTALL)
			{
				if (bExists)
				{
					if (pIter->m_isInstalled == INSTALLSTATE_LOCAL)
					{
						pIter->m_iAttributes &= ~SCAUR_FAIL_IF_EXISTS;
					}
					if ((pIter->m_iAttributes & SCAUR_FAIL_IF_EXISTS) &&
						!(pIter->m_iAttributes & SCAUR_UPDATE_IF_EXISTS))
					{
						LogErrorMessage (_T("Failed to create url reservation - already exists."));
						throw _com_error (HRESULT_FROM_WIN32 (NERR_GroupExists));
					}
				}
				
				//if (!bExists || pIter->m_iAttributes & SCAU_DONT_CREATE_USER)
				//{
				//	DoDeferredAction (_T("CreateUserExRollback"), strCAData, COST_USER_DELETE);
				//}

				// Schedule create now
				DoDeferredAction (_T("CreateUrlReservation"), strCAData, COST_USER_ADD);
			}
			else if (pIter->m_todoComponent == WCA_TODO_UNINSTALL && bExists &&
				!(SCAUR_DONT_REMOVE_ON_UNINSTALL & (pIter->m_iAttributes)))
			{
				DoDeferredAction (_T("CreateUrlReservationRollback"), strCAData, COST_USER_DELETE);
			}
		}
	}
}*/

void CUrlReservationInstaller::ExecuteUrlReservation ()
{
	USES_CONVERSION;
	CString strData = GetProperty (_T("CustomActionData"));

	// Loop through CA data
	while (!strData.IsEmpty ())
	{
		// Extract data for next reservation
		CString strTodo = ExtractNextTabDelimitedBlock (strData);
		CString strUrl = ExtractNextTabDelimitedBlock (strData);
		CString strAttributes = ExtractNextTabDelimitedBlock (strData);
		int iAttributes = _ttoi (strAttributes);
		CString strAcl = ExtractNextTabDelimitedBlock (strData);

		// Determine component install mode
		WCA_TODO todo = GetTranslatedInstallMode ((WCA_TODO)_ttoi (strTodo));

		// Execute sub-action
		switch (todo)
		{
		case WCA_TODO_INSTALL:
		case WCA_TODO_REINSTALL:
			AddUrlReservation (strUrl, iAttributes, strAcl);
			break;

		case WCA_TODO_UNINSTALL:
			RemoveUrlReservation (strUrl);
			break;
		}
	}
}

void CUrlReservationInstaller::AddUrlReservation (LPCTSTR pszUrl, int iAttributes, LPCTSTR pszAcl)
{
    HRESULT hr = S_OK;
	LPBYTE pQueryResult = NULL;
	bool bTerminateHttp = false;
	try
	{
		USES_CONVERSION;

		// Read in custom data
		CString strUrl (pszUrl);
		CString strAcl (pszAcl);
		LogInfo (_T("[Url=%s, Attrib=%d, Acl=%s]"), strUrl, iAttributes, strAcl);

		// Create discretionary access control list from CA ACL parts.
		CDacl dacl;
		while (!strAcl.IsEmpty())
		{
			CString strName = ExtractNextBarDelimitedBlock (strAcl);
			CString strDomain = ExtractNextBarDelimitedBlock (strAcl);
			CString strAttrib = ExtractNextBarDelimitedBlock (strAcl);
			int iAclAttributes = _ttoi (strAttrib);

			ACCESS_MASK mask = 0;
			if ((iAclAttributes & SCAURA_CAN_REGISTER) != 0)
			{
				mask |= GENERIC_EXECUTE;
			}
			if ((iAclAttributes & SCAURA_CAN_REGISTER) != 0)
			{
				mask |= GENERIC_WRITE;
			}

			// Form the fully qualified user name
			CString strFQUserName;
			if (strDomain.GetLength() > 0)
			{
				strFQUserName.Format (_T("%s\\%s"), strDomain, strName);
			}
			else
			{
				strFQUserName = strName;
			}

			// Get the user sid
			CSid sid (strFQUserName);
			dacl.AddAllowedAce (sid, mask);
		}

		// Create security descriptor
		CSecurityDesc sd;
		sd.SetDacl (dacl);

		// TODO: Setup owner and group for security descriptor

		// Convert into SD string.
		CString strSddl;
		sd.MakeSelfRelative();
		sd.ToString (&strSddl);

		// Prepare URL Reservation struct.
		HTTP_SERVICE_CONFIG_URLACL_SET urlacl;
		urlacl.KeyDesc.pUrlPrefix = const_cast<PWSTR> (T2CW (strUrl));
		urlacl.ParamDesc.pStringSecurityDescriptor = const_cast<PWSTR> (T2CW (strSddl));

		// Initialise HTTP API
		HTTPAPI_VERSION tApiVersion = HTTPAPI_VERSION_1;
		ULONG lResult = HttpInitialize (tApiVersion, HTTP_INITIALIZE_CONFIG, 0);
		if (lResult != NO_ERROR)
		{
			LogError (_T("Failed to initialise HTTP API - %08lX."), lResult);
			throw _com_error(HRESULT_FROM_WIN32(lResult));
		}

		// We must terminate HTTP on the way out
		bTerminateHttp = true;

		// Query for URL
		DWORD dwLength = 0;
		HTTP_SERVICE_CONFIG_URLACL_QUERY tQuery;
		tQuery.QueryDesc = HttpServiceConfigQueryExact;
		tQuery.KeyDesc.pUrlPrefix = const_cast<PWSTR> (T2CW(strUrl));
		lResult = HttpQueryServiceConfiguration (
			0,
			HttpServiceConfigUrlAclInfo,
			&tQuery,
			sizeof (tQuery),
			pQueryResult,
			dwLength,
			&dwLength,
			NULL);
		if (lResult == ERROR_INSUFFICIENT_BUFFER)
		{
			pQueryResult = new BYTE[dwLength];
			lResult = HttpQueryServiceConfiguration (
				0,
				HttpServiceConfigUrlAclInfo,
				&tQuery,
				sizeof (tQuery),
				pQueryResult,
				dwLength,
				&dwLength,
				NULL);
		}

		bool bCreateReservation = false;
		if (lResult == NO_ERROR)
		{
			// If entry exists and we are allowed to update then delete
			if ((iAttributes & SCAUR_UPDATE_IF_EXISTS) != 0)
			{
				lResult = HttpDeleteServiceConfiguration (
					0,
					HttpServiceConfigUrlAclInfo,
					pQueryResult,
					dwLength,
					NULL);
				if (lResult != NO_ERROR)
				{
					LogError(_T("Failed to delete old url reservation [%s]. ErrorCode = %08lX"), lResult);
					throw _com_error(HRESULT_FROM_WIN32(lResult));
				}
				bCreateReservation = true;
			}
			else if ((iAttributes & SCAUR_FAIL_IF_EXISTS) != 0)
			{
				throw _com_error(E_FAIL);
			}
		}
		else
		{
			bCreateReservation = true;
		}

		if (bCreateReservation)
		{
			lResult = HttpSetServiceConfiguration (
				0,
				HttpServiceConfigUrlAclInfo,
				&urlacl,
				sizeof(urlacl),
				NULL);
			if (lResult != NO_ERROR)
			{
				LogError(_T("Failed to set url reservation [%s]. ErrorCode = %08lX"), lResult);
				throw _com_error(HRESULT_FROM_WIN32(lResult));
			}
		}
	}
	catch(const _com_error& error)
	{
		if (pQueryResult != NULL)
		{
			delete pQueryResult;
		}
		if (bTerminateHttp)
		{
			HttpTerminate (HTTP_INITIALIZE_CONFIG, 0);
		}
		throw error;
	}
	if (pQueryResult != NULL)
	{
		delete pQueryResult;
	}
	if (bTerminateHttp)
	{
		HttpTerminate (HTTP_INITIALIZE_CONFIG, 0);
	}

	ProgressMessage (COST_URLRESERVATION_ADD, false);
}

void CUrlReservationInstaller::RemoveUrlReservation (LPCTSTR pszUrl)
{
    UINT er = ERROR_SUCCESS;
	HRESULT hr = S_OK;
	bool bTerminateHttp = false;
	LPBYTE pQueryResult = NULL;
	try
	{
		USES_CONVERSION;
	
		// Read in custom data
		CString strUrl (pszUrl);
		LogInfo (_T("[Url=%s]"), strUrl);

		// Initialise HTTP API
		HTTPAPI_VERSION tApiVersion = HTTPAPI_VERSION_1;
		ULONG lResult = HttpInitialize (tApiVersion, HTTP_INITIALIZE_CONFIG, 0);
		if (lResult != NO_ERROR)
		{
			LogError (_T("Failed to initialise HTTP API - %08lX."), lResult);
			throw _com_error(HRESULT_FROM_WIN32(lResult));
		}

		// We must terminate HTTP on the way out
		bTerminateHttp = true;

		// Query for URL
		DWORD dwLength = 0;
		HTTP_SERVICE_CONFIG_URLACL_QUERY tQuery;
		tQuery.QueryDesc = HttpServiceConfigQueryExact;
		tQuery.KeyDesc.pUrlPrefix = const_cast<PWSTR> (T2CW(strUrl));
		lResult = HttpQueryServiceConfiguration (
			0,
			HttpServiceConfigUrlAclInfo,
			&tQuery,
			sizeof (tQuery),
			pQueryResult,
			dwLength,
			&dwLength,
			NULL);
		if (lResult == ERROR_INSUFFICIENT_BUFFER)
		{
			pQueryResult = new BYTE[dwLength];
			lResult = HttpQueryServiceConfiguration (
				0,
				HttpServiceConfigUrlAclInfo,
				&tQuery,
				sizeof (tQuery),
				pQueryResult,
				dwLength,
				&dwLength,
				NULL);
		}

		if (lResult == NO_ERROR)
		{
			lResult = HttpDeleteServiceConfiguration (
				0,
				HttpServiceConfigUrlAclInfo,
				pQueryResult,
				dwLength,
				NULL);
			if (lResult != NO_ERROR)
			{
				LogError(_T("Failed to delete url reservation [%s]. ErrorCode = %08lX"), lResult);
				throw _com_error(HRESULT_FROM_WIN32(lResult));
			}
		}
	}
	catch (const _com_error& error)
	{
		if (pQueryResult != NULL)
		{
			delete pQueryResult;
		}
		if (bTerminateHttp)
		{
			HttpTerminate (HTTP_INITIALIZE_CONFIG, 0);
		}

		LogError (_T("failed to delete url reservation: %08lX"), error.Error ());
		throw error;
	}
	if (pQueryResult != NULL)
	{
		delete pQueryResult;
	}
	if (bTerminateHttp)
	{
		HttpTerminate (HTTP_INITIALIZE_CONFIG, 0);
	}

	// Update progress
	ProgressMessage (COST_URLRESERVATION_DELETE, false);
}

bool CUrlReservationInstaller::GetUrlReservationExists (LPCTSTR pszUrl)
{
	bool bExists = false;
	USES_CONVERSION;

	HTTPAPI_VERSION tApiVersion = HTTPAPI_VERSION_1;
	ULONG lResult = HttpInitialize (tApiVersion, HTTP_INITIALIZE_CONFIG, 0);
	if (lResult == NO_ERROR)
	{
		LPBYTE pQueryResult = NULL;
		try
		{
			DWORD dwLength = 0;
			HTTP_SERVICE_CONFIG_URLACL_QUERY tQuery;
			tQuery.QueryDesc = HttpServiceConfigQueryExact;
			tQuery.KeyDesc.pUrlPrefix = const_cast<PWSTR> (T2CW(pszUrl));
			lResult = HttpQueryServiceConfiguration (
				0,
				HttpServiceConfigUrlAclInfo,
				&tQuery,
				sizeof (tQuery),
				pQueryResult,
				dwLength,
				&dwLength,
				NULL);
			if (lResult == ERROR_INSUFFICIENT_BUFFER)
			{
				pQueryResult = new BYTE[dwLength];
				lResult = HttpQueryServiceConfiguration (
					0,
					HttpServiceConfigUrlAclInfo,
					&tQuery,
					sizeof (tQuery),
					pQueryResult,
					dwLength,
					&dwLength,
					NULL);
			}

			if (lResult == NO_ERROR)
			{
				bExists = true;
			}
			if (lResult == ERROR_NO_MORE_ITEMS)
			{
				bExists = false;
			}
		}
		catch(...)
		{
		}

		if (pQueryResult != NULL)
		{
			delete pQueryResult;
		}

		HttpTerminate (HTTP_INITIALIZE_CONFIG, 0);
	}
	return bExists;
}
