#include "stdafx.h"
#include "GroupInstaller.h"

LPCTSTR vActionableGroupQuery = _T("SELECT `Group`, `Component_`, `Name`, `Domain`, `Description`, `Attributes` FROM `GroupEx`");
enum eActionableGroupQuery { vgqGroup = 1, vgqComponent, vgqName, vgqDomain, vgqDescription, vgqAttributes };

extern "C" UINT __stdcall SchedGroupsInstall (MSIHANDLE hInstall)
{
	CGroupInstaller tHelper (hInstall);
	try
	{
		tHelper.ScheduleGroups (WCA_TODO_INSTALL);
		return 0;
	}
	catch (const _com_error& e)
	{
		tHelper.LogError (_T("Failed with error %08lx"), e.Error ());
		return e.WCode ();
	}
}

extern "C" UINT __stdcall SchedGroupsUninstall (MSIHANDLE hInstall)
{
	CGroupInstaller tHelper (hInstall);
	try
	{
		tHelper.ScheduleGroups (WCA_TODO_UNINSTALL);
		return 0;
	}
	catch (const _com_error& e)
	{
		tHelper.LogError (_T("Failed with error %08lx"), e.Error ());
		return e.WCode ();
	}
}

extern "C" UINT __stdcall ExecGroup(MSIHANDLE hInstall)
{
	CGroupInstaller tHelper (hInstall);
	try
	{
		tHelper.ExecuteGroup();
	}
	catch(const _com_error& error)
	{
		tHelper.LogError (_T("Caught exception. [HR=%08lX]"), error.Error ());
		return ERROR_INSTALL_FAILURE;
	}
	return ERROR_SUCCESS;
}

CGroupInstaller::CGroupInstaller (MSIHANDLE hInstall)
	: CPrincipalInstaller (hInstall)
{
}

void CGroupInstaller::ScheduleGroups (WCA_TODO todoScheduled)
{
	try
	{
		// Check we actually have a table...
		if (!GetActiveDatabase().IsTable (_T("GroupEx")))
		{
			return;
		}

		LogInfo (_T("ScheduleGroups - Pending Open/Exec View"));
		CMsiView view (GetActiveDatabase().OpenExecuteView (vActionableGroupQuery));

		CString strAllCAData;
		int iInstanceCount = 0;

		LogInfo (_T("ScheduleGroups - Pending Fetch Loop"));
		CMsiRecord record;
		while (view.Fetch (record))
		{
			LogInfo (_T("ScheduleGroups - Inner Loop - Pending Component Check"));
			CString strComponent = record.GetString (vgqComponent);
			WCA_TODO todoComponent = GetComponentToDo (strComponent);
			if ((todoComponent == WCA_TODO_REINSTALL ? WCA_TODO_INSTALL : todoComponent) != todoScheduled)
			{
				LogInfo (_T("Component '%s' action state (%d) doesn't match request (%d)"),
					strComponent, todoComponent, todoScheduled);
				continue;
			}

			LogInfo (_T("ScheduleGroups - Pending Group Add"));
			CString strKey = record.GetString (vgqGroup);
			CString strName = GetRecordFormattedString (record, vgqName);
			CString strDomain = GetRecordFormattedString (record, vgqDomain);
			CString strDescription = GetRecordFormattedString (record, vgqDescription);
			int iAttributes = record.GetInteger (vgqAttributes);

			CString strResult;
			strResult.Format (_T("%d\t%s\t%s\t%d\t%s"),
				todoComponent, (LPCTSTR)strName, (LPCTSTR)strDomain, 
				iAttributes, (LPCTSTR)strDescription);
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

		if (strAllCAData.IsEmpty ())
		{
			if (todoScheduled == WCA_TODO_INSTALL)
			{
				DoDeferredAction (_T("SuiExecGroupsInstall"), strAllCAData, iInstanceCount * COST_GROUP_ADD); 
				DoDeferredAction (_T("SuiRollbackGroupsInstall"), strAllCAData, iInstanceCount * COST_GROUP_ADD); 
			}
			else
			{
				DoDeferredAction (_T("SuiExecGroupsUninstall"), strAllCAData, iInstanceCount * COST_GROUP_DELETE); 
				DoDeferredAction (_T("SuiRollbackGroupsUninstall"), strAllCAData, iInstanceCount * COST_GROUP_DELETE); 
			}
		}
	}
	catch (const _com_error& e)
	{
		LogError (_T("Exception caught while reading groups: %08lX"),
			e.Error ());
		throw e;
	}
}

void CGroupInstaller::ExecuteGroup ()
{
	USES_CONVERSION;
	CString strData = GetProperty (_T("CustomActionData"));

	// Determine component install mode
	CString strTodo = ExtractNextTabDelimitedBlock (strData);
	WCA_TODO todo = GetTranslatedInstallMode ((WCA_TODO)_ttoi (strTodo));

	while (!strData.IsEmpty ())
	{
		// Read custom action data
		CString strName = ExtractNextTabDelimitedBlock (strData);
		CString strDomain = ExtractNextTabDelimitedBlock (strData);
		CString strAttrib = ExtractNextTabDelimitedBlock (strData);
		CString strDescription = ExtractNextTabDelimitedBlock (strData);
		int iAttributes = _ttoi (strAttrib);

		// Execute sub-action
		switch (todo)
		{
		case WCA_TODO_INSTALL:
		case WCA_TODO_REINSTALL:
			AddGroup (strName, strDomain, iAttributes, strDescription);
			break;

		case WCA_TODO_UNINSTALL:
			RemoveGroup (strName, strDomain, iAttributes);
			break;
		}
	}
}

void CGroupInstaller::AddGroup (LPCTSTR pszName, LPCTSTR pszDomain, int iAttributes, LPCTSTR pszDescription)
{
	UINT er = ERROR_SUCCESS;
	HRESULT hr = S_OK;
	LOCALGROUP_INFO_1* pGroupInfo = NULL;
	try
	{
		USES_CONVERSION;
		CString strName (pszName);
		CString strDomain (pszDomain);
		CString strDescription (pszDescription);
		LogInfo (_T("[Name=%s, Domain=%s, Attrib=%d, Desc=%s]"),
			strName, strDomain, iAttributes, strDescription);

		LOCALGROUP_INFO_1 groupInfo;
		if (!(SCAG_DONT_CREATE_GROUP & iAttributes))
		{
			::ZeroMemory(&groupInfo, sizeof(GROUP_INFO_1));
			groupInfo.lgrpi1_name = strName.AllocSysString();
			groupInfo.lgrpi1_comment = strDescription.AllocSysString();

			//
			// Create the Group
			//
			CString strDomainOrController = GetDomainOrControllerName (pszDomain);

			DWORD dw;
			er = NetLocalGroupAdd (T2CW(strDomainOrController), 1, reinterpret_cast<LPBYTE>(&groupInfo), &dw);
			LogInfo (_T("NetLocalGroupAdd returned %04lX and error param %08lX."), er, dw);
			if (er == NERR_GroupExists || er == ERROR_ALIAS_EXISTS)
			{
				if (SCAG_UPDATE_IF_EXISTS & iAttributes)
				{
					er = NetLocalGroupGetInfo(T2CW(strDomainOrController), T2CW(strName), 1, reinterpret_cast<LPBYTE*>(&pGroupInfo));
					if (NERR_Success == er)
					{
						pGroupInfo->lgrpi1_comment = (LPWSTR)T2CW(strDescription);
						er = NetLocalGroupSetInfo (T2CW(strDomainOrController), T2CW(strName), 1, reinterpret_cast<LPBYTE>(pGroupInfo), &dw);
					}
				}
				else if (!(SCAG_FAIL_IF_EXISTS & iAttributes))
				{
					er = NERR_Success;
				}
			}

			// Throw if we've failed
			hr = HRESULT_FROM_WIN32(er);
			if (FAILED (hr))
			{
				LogError (_T("failed to create group: %s"), strName);
				throw _com_error (hr);
			}
		}
	}
	catch(const _com_error& error)
	{
		if (pGroupInfo)
		{
			::NetApiBufferFree((LPVOID)pGroupInfo);
		}
		throw error;
	}
	if (pGroupInfo)
	{
		::NetApiBufferFree((LPVOID)pGroupInfo);
	}

	// Update progress
	ProgressMessage (COST_GROUP_ADD, false);
}

void CGroupInstaller::RemoveGroup (LPCTSTR pszName, LPCTSTR pszDomain, int iAttributes)
{
    UINT er = ERROR_SUCCESS;
	HRESULT hr = S_OK;
	try
	{
		USES_CONVERSION;
		CString strName (pszName);
		CString strDomain (pszDomain);
		LogInfo (_T("[Name=%s, Domain=%s, Attrib=%d]"),
			strName, strDomain, iAttributes);

		if (!(SCAG_DONT_CREATE_GROUP & iAttributes))
		{
			CString strDomainOrController = GetDomainOrControllerName (pszDomain);

			er = ::NetLocalGroupDel (T2CW(strDomainOrController), strName.AllocSysString ());
			if (NERR_GroupNotFound == er)
			{
				er = NERR_Success;
			}
			hr = HRESULT_FROM_WIN32(er);
			if (FAILED (hr))
			{
				throw _com_error (hr);
			}
		}
	}
	catch (const _com_error& error)
	{
		LogError (_T("failed to delete group: %08lX"), error.Error ());
		throw error;
	}

	// Update progress
	ProgressMessage (COST_GROUP_DELETE, false);
}

bool CGroupInstaller::GetGroupExists (LPCTSTR pszName, LPCTSTR pszDomain)
{
	USES_CONVERSION;
	UINT er;
	CString strDomainOrController = GetDomainOrControllerName (pszDomain);

	LOCALGROUP_INFO_1* pGroupInfo = NULL;
	er = NetLocalGroupGetInfo (T2CW(strDomainOrController), T2CW(pszName), 1, (LPBYTE*)&pGroupInfo);
	if (pGroupInfo != NULL)
	{
		::NetApiBufferFree(static_cast<LPVOID>(pGroupInfo));
	}

	bool bExists = false;
	if (er == NERR_Success)
	{
		bExists = true;
	}
	else if (er != NERR_GroupNotFound)
	{
		throw _com_error (HRESULT_FROM_WIN32(er));
	}
	return bExists;
}
