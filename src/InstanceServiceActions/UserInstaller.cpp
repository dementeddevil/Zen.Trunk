#include "stdafx.h"
#include "UserInstaller.h"

// {27636B00-410F-11CF-B1FF-02608C9E7553}
static const GUID IID_IADsGroup = 
{ 0x27636B00, 0x410F, 0x11CF, { 0xB1, 0xFF, 0x02, 0x60, 0x8C, 0x9E, 0x75, 0x53 } };

LPCTSTR vActionableUserQuery = _T("SELECT `User`, `Component_`, `Name`, `Domain`, `Password`, `Attributes` FROM `UserEx`");
enum eActionableUserQuery { vuqUser = 1, vuqComponent, vuqName, vuqDomain, vuqPassword, vuqAttributes };

LPCTSTR vLookupUserGroupQuery = _T("SELECT `User_`, `Group_` FROM `UserGroup` WHERE `User_` = ?");
enum eLookupUserGroupQuery { vlugqUser = 1, vlugqGroup };

LPCTSTR vLookupGroupQuery = _T("SELECT `Group`, `Name`, `Domain` FROM `Group` WHERE `Group` = ?");
enum eLookupGroupQuery { vlgqGroup = 1, vlgqName, vlgqDomain };

extern "C" UINT __stdcall SchedUsersInstall (MSIHANDLE hInstall)
{
	CUserInstaller tHelper (hInstall);
	try
	{
		tHelper.ScheduleUsers (WCA_TODO_INSTALL);
		return 0;
	}
	catch (const _com_error& e)
	{
		tHelper.LogError (_T("Failed with error %08lx"), e.Error ());
		return e.WCode ();
	}
}

extern "C" UINT __stdcall SchedUsersUninstall (MSIHANDLE hInstall)
{
	CUserInstaller tHelper (hInstall);
	try
	{
		tHelper.ScheduleUsers (WCA_TODO_UNINSTALL);
		return 0;
	}
	catch (const _com_error& e)
	{
		tHelper.LogError (_T("Failed with error %08lx"), e.Error ());
		return e.WCode ();
	}
}

extern "C" UINT __stdcall ExecUser(MSIHANDLE hInstall)
{
	CUserInstaller tHelper (hInstall);
	try
	{
		tHelper.ExecuteUser();
	}
	catch(const _com_error& error)
	{
		tHelper.LogError (_T("Caught exception. [HR=%08lX]"), error.Error ());
		return ERROR_INSTALL_FAILURE;
	}
	return ERROR_SUCCESS;
}

CUserInstaller::CUserInstaller (MSIHANDLE hInstall)
	: CPrincipalInstaller (hInstall)
{
}

void CUserInstaller::ScheduleUsers (WCA_TODO todoScheduled)
{
	try
	{
		// Check we actually have a table...
		if (!GetActiveDatabase().IsTable (_T("UserEx")))
		{
			return;
		}

		LogInfo (_T("ScheduleUsers - Pending Open/Exec View"));
		CMsiView view (GetActiveDatabase().OpenExecuteView (vActionableUserQuery));

		CString strAllCAData;
		int iInstanceCount = 0;

		LogInfo (_T("ScheduleUsers - Pending Fetch Loop"));
		CMsiRecord record;
		while (view.Fetch (record))
		{
			LogInfo (_T("ScheduleUsers - Inner Loop - Pending Component Check"));
			CString strComponent = record.GetString (vuqComponent);
			WCA_TODO todoComponent = GetComponentToDo (strComponent);
			if ((todoComponent == WCA_TODO_REINSTALL ? WCA_TODO_INSTALL : todoComponent) != todoScheduled)
			{
				LogInfo (_T("Component '%s' action state (%d) doesn't match request (%d)"),
					strComponent, todoComponent, todoScheduled);
				continue;
			}

			CString strKey = record.GetString (vuqUser);
			CString strName = GetRecordFormattedString (record, vuqName);
			CString strDomain = GetRecordFormattedString (record, vuqDomain);
			CString strPassword = GetRecordFormattedString (record, vuqPassword);
			int iAttributes = record.GetInteger (vuqAttributes);
			CString strMembership, strResult;

			LogInfo (_T("ScheduleUsers - Pending Open/Exec UserGroup View"));
			CMsiRecord userGroupParam (1);
			userGroupParam.SetString (1, strKey);
			CMsiView membershipView (GetActiveDatabase().OpenExecuteView (
				vLookupUserGroupQuery, userGroupParam));
			CMsiRecord membershipRecord;
			while (membershipView.Fetch (membershipRecord))
			{
				// Obtain group key
				LogInfo (_T("ScheduleUsers - Pending UserGroup Read"));
				CString strGroupKey = membershipRecord.GetString (vlugqGroup);

				// Fire query to retrieve the group information
				LogInfo (_T("ScheduleUsers - Pending Open/Exec Group View"));
				CMsiRecord groupParam (1);
				groupParam.SetString (1, strGroupKey);
				CMsiView groupView (GetActiveDatabase().OpenExecuteView (
					vLookupGroupQuery, groupParam));
				CMsiRecord groupRecord;
				groupView.FetchSingleRecord (groupRecord);

				LogInfo (_T("ScheduleUsers - Pending Group Read"));
				CString strMemberGroup = GetRecordFormattedString (groupRecord, vlgqName);
				CString strMemberDomain = GetRecordFormattedString (groupRecord, vlgqDomain);

				// Build CA data block
				strResult.Format (_T("%s|%s"), 
					strMemberGroup, strMemberDomain);

				// Append to ACL information
				if (strMembership.IsEmpty())
				{
					strMembership = strResult;
				}
				else
				{
					strMembership.AppendFormat (_T("|%s"), strResult);
				}
			}

			strResult.Format (_T("%d\t%s\t%s\t%d\t%s\t%s"),
				todoComponent, (LPCTSTR)strName, (LPCTSTR)strDomain,
				iAttributes, (LPCTSTR)strPassword, (LPCTSTR)strMembership);

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

		if (!strAllCAData.IsEmpty ())
		{
			if (todoScheduled == WCA_TODO_INSTALL)
			{
				DoDeferredAction (_T("SuiExecUsersInstall"), strAllCAData, iInstanceCount * COST_USER_ADD); 
				DoDeferredAction (_T("SuiRollbackUsersInstall"), strAllCAData, iInstanceCount * COST_USER_ADD); 
			}
			else
			{
				DoDeferredAction (_T("SuiExecUsersUninstall"), strAllCAData, iInstanceCount * COST_USER_DELETE); 
				DoDeferredAction (_T("SuiRollbackUsersUninstall"), strAllCAData, iInstanceCount * COST_USER_DELETE); 
			}
		}
	}
	catch (const _com_error& e)
	{
		LogError (_T("Exception caught while reading users: %08lX"),
			e.Error ());
		throw e;
	}
}

void CUserInstaller::ExecuteUser ()
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
		int iAttributes = _ttoi (strAttrib);
		CString strPassword = ExtractNextTabDelimitedBlock (strData);
		CString strMembership = ExtractNextTabDelimitedBlock (strData);

		// Execute sub-action
		switch (todo)
		{
		case WCA_TODO_INSTALL:
		case WCA_TODO_REINSTALL:
			AddUser (strName, strDomain, iAttributes, strPassword, strMembership);
			break;

		case WCA_TODO_UNINSTALL:
			RemoveUser (strName, strDomain, iAttributes, strMembership);
			break;
		}
	}
}

void CUserInstaller::AddUser (LPCTSTR pszName, LPCTSTR pszDomain, int iAttributes, LPCTSTR pszPassword, LPCTSTR pszMembership)
{
    UINT er = ERROR_SUCCESS;
    HRESULT hr = S_OK;
    PDOMAIN_CONTROLLER_INFO pDomainControllerInfo = NULL;
    USER_INFO_1* pUserInfo = NULL;
	try
	{
		USES_CONVERSION;
		CString strName (pszName);
		CString strDomain (pszDomain);
		CString strPassword (pszPassword);
		CString strMembership (pszMembership);
		LogInfo (_T("[Name=%s, Domain=%s, Attrib=%d, Password=%s, Membership=%s]"),
			strName, strDomain, iAttributes, CString('*', strPassword.GetLength()), strMembership);

		// Form the fully qualified domain user name.
		CString strFQUserName;
		if (strDomain.GetLength() > 0)
		{
			strFQUserName.Format (_T("%s\\%s"), strDomain, strName);
		}
		else
		{
			strFQUserName = strName;
		}

		// Get the user SID
		CSid sid (strFQUserName);

		// Test for service account SIDs
		bool fServiceAccount = false;
		fServiceAccount = IsWellKnownServiceSid(sid);
		if ((!fServiceAccount) || (!(SCAU_IGNORE_SERVICE_ACCOUNTS & iAttributes)))
		{
			USER_INFO_1 userInfo;
			if (!(SCAU_DONT_CREATE_USER & iAttributes))
			{
				::ZeroMemory(&userInfo, sizeof(USER_INFO_1));
				userInfo.usri1_name = strName.AllocSysString();
				userInfo.usri1_priv = USER_PRIV_USER;
				userInfo.usri1_flags = UF_SCRIPT;
				userInfo.usri1_home_dir = NULL;
				userInfo.usri1_comment = NULL;
				userInfo.usri1_script_path = NULL;
				SetUserPasswordAndAttributes(&userInfo, strPassword.AllocSysString(), iAttributes);

				//
				// Create the User
				//
				CString strDomainOrController = GetDomainOrControllerName (pszDomain);

				DWORD dw;
				er = NetUserAdd(T2CW(strDomainOrController), 1, reinterpret_cast<LPBYTE>(&userInfo), &dw);
				LogInfo (_T("NetUserAdd returned %04lX and error param %08lX."), er, dw);
				if (NERR_UserExists == er)
				{
					if (SCAU_UPDATE_IF_EXISTS & iAttributes)
					{
						er = NetUserGetInfo(T2CW(strDomainOrController), T2CW(strName), 1, reinterpret_cast<LPBYTE*>(&pUserInfo));
						if (NERR_Success == er)
						{
							// Change the existing user's password and attributes again then try 
							// to update user with this new data
							SetUserPasswordAndAttributes(pUserInfo, strPassword.AllocSysString(), iAttributes);
							er = NetUserSetInfo (T2CW(strDomainOrController), T2CW(strName), 1, reinterpret_cast<LPBYTE>(pUserInfo), &dw);
						}
					}
					else if (!(SCAU_FAIL_IF_EXISTS & iAttributes))
					{
						er = NERR_Success;
					}
				}
				else if (NERR_PasswordTooShort == er || NERR_PasswordTooLong == er)
				{
					hr = HRESULT_FROM_WIN32(er);
					LogError (_T("failed to create user: %s due to invalid password."), strName);
					throw _com_error (hr);
				}

				hr = HRESULT_FROM_WIN32(er);
				if (FAILED (hr))
				{
					LogError (_T("failed to create user: %s"), strName);
					throw _com_error (hr);
				}
			}

			if (SCAU_ALLOW_LOGON_AS_SERVICE & iAttributes)
			{
				ModifyUserLocalServiceRight(strDomain, strName, true);
			}

			//
			// Add the users to groups
			//
			while (!strMembership.IsEmpty ())
			{
				CString strGroup = ExtractNextBarDelimitedBlock(strMembership);
				CString strGroupDomain = ExtractNextBarDelimitedBlock(strMembership);

				if (strGroup.GetLength () > 0)
				{
					AddUserToGroup(strName, strDomain, strGroup, strGroupDomain);
				}
			}
		}
	}
	catch(const _com_error& error)
	{
		if (pUserInfo)
		{
			::NetApiBufferFree((LPVOID)pUserInfo);
		}
		if (pDomainControllerInfo)
		{
			::NetApiBufferFree((LPVOID)pDomainControllerInfo);
		}
		throw error;
	}
	if (pUserInfo)
	{
		::NetApiBufferFree((LPVOID)pUserInfo);
	}
	if (pDomainControllerInfo)
	{
		::NetApiBufferFree((LPVOID)pDomainControllerInfo);
	}

	ProgressMessage (COST_USER_ADD, false);
}

void CUserInstaller::RemoveUser (LPCTSTR pszName, LPCTSTR pszDomain, int iAttributes, LPCTSTR pszMembership)
{
    UINT er = ERROR_SUCCESS;
	HRESULT hr = S_OK;
	try
	{
		USES_CONVERSION;
		CString strName (pszName);
		CString strDomain (pszDomain);
		CString strMembership (pszMembership);
		LogInfo (_T("[Name=%s, Domain=%s, Attrib=%d, Membership=%s]"),
			strName, strDomain, iAttributes, strMembership);

		// Form the fully qualified domain user name.
		CString strFQUserName;
		if (strDomain.GetLength() > 0)
		{
			strFQUserName.Format (_T("%s\\%s"), strDomain, strName);
		}
		else
		{
			strFQUserName = strName;
		}

		// Get the user SID
		CSid sid (strFQUserName);

		// Test for service account SIDs
		bool fServiceAccount = false;
		fServiceAccount = IsWellKnownServiceSid(sid);
		if ((!fServiceAccount) || (!(SCAU_IGNORE_SERVICE_ACCOUNTS & iAttributes)))
		{
			//
			// Remove the logon as service privilege.
			//
			if (SCAU_ALLOW_LOGON_AS_SERVICE & iAttributes)
			{
				ModifyUserLocalServiceRight(strDomain, strName, false);
			}

			//
			// Remove the User Account if the user was created by us.
			//
			if (!(SCAU_DONT_CREATE_USER & iAttributes))
			{
				CString strDomainOrController = GetDomainOrControllerName (pszDomain);

				er = ::NetUserDel (T2CW(strDomainOrController), strName.AllocSysString ());
				if (NERR_UserNotFound == er)
				{
					er = NERR_Success;
				}
				hr = HRESULT_FROM_WIN32(er);
				if (FAILED (hr))
				{
					LogError (_T("failed to delete user account: %s"), strName);
					throw _com_error (hr);
				}
			}
			else
			{
				while (!strMembership.IsEmpty ())
				{
					// Fetch group and group domain
					CString strGroup = ExtractNextBarDelimitedBlock(strMembership);
					CString strGroupDomain = ExtractNextBarDelimitedBlock(strMembership);

					if (strGroup.GetLength () > 0)
					{
						RemoveUserFromGroup(strName, strDomain, strGroup, strGroupDomain);
					}
				}
			}
		}
	}
	catch (const _com_error& error)
	{
		LogError (_T("failed to delete user: %08lX"), error.Error ());
		throw error;
	}

	// Update progress
	ProgressMessage (COST_USER_DELETE, false);
}

void CUserInstaller::AddUserToGroup(
    const CString& strUser,
    const CString& strUserDomain,
    const CString& strGroup,
    const CString& strGroupDomain)
{
	USES_CONVERSION;
	try
	{
		// Get group domain pointer if possible
		LPCWSTR pwzGroupDomain = NULL;
		if (strGroupDomain.GetLength() > 0)
		{
			pwzGroupDomain = T2CW(strGroupDomain);
		}

		// Try adding it to the global group first
		UINT ui = ::NetGroupAddUser(pwzGroupDomain, T2CW(strGroup), T2CW(strUser));
		if (NERR_GroupNotFound == ui)
		{
			// Build combined domain user name as required.
			CString strLocalUser;
			if (strUserDomain.GetLength() > 0)
			{
				strLocalUser.Format (_T("%s\\%s"), strUserDomain, strUser);
			}
			else
			{
				strLocalUser = strUser;
			}

			// Attempt to add to local group
			LOCALGROUP_MEMBERS_INFO_3 lgmi;
			lgmi.lgrmi3_domainandname = const_cast<LPWSTR>(T2CW(strLocalUser));
			ui = ::NetLocalGroupAddMembers(T2CW(strGroupDomain), T2CW(strGroup),
				3, reinterpret_cast<LPBYTE>(&lgmi), 1);
		}

		HRESULT hr = S_OK;
		if (ui == ERROR_MEMBER_IN_ALIAS)
		{
			// already a member of the group don't report an error
			hr = S_OK;
		}
		else
		{
			hr = HRESULT_FROM_WIN32(ui);
		}

		//
		// If we failed, try active directory
		//
		if (FAILED(hr))
		{
			LogWarning (_T("Failed to add user: %S, domain %S to group: %S, domain: %S with error 0x%x.  Attempting to use Active Directory"),
				strUser, strUserDomain, strGroup, strGroupDomain, hr);

			CString strADUser, strADGroup;
			strADUser.Format (_T("WinNT://%s/%s,user"), strUserDomain, strUser);
			strADGroup.Format (_T("WinNT://%s/%s,group"), strGroupDomain, strGroup);

			IADsGroup *pGroup = NULL;
			hr = ::ADsGetObject(strADGroup.AllocSysString(), IID_IADsGroup, 
				reinterpret_cast<void**>(&pGroup));
			if (FAILED (hr))
			{
				LogError (_T("Failed to get group '%s'."), strADGroup);
				throw _com_error (hr);
			}

			hr = pGroup->Add(strADUser.AllocSysString());
			if ((HRESULT_FROM_WIN32(ERROR_OBJECT_ALREADY_EXISTS) == hr) || 
				(HRESULT_FROM_WIN32(ERROR_MEMBER_IN_ALIAS) == hr))
			{
				hr = S_OK;
			}
			if (FAILED (hr))
			{
				LogError (_T("Failed to add user %s to group '%s'."), strADUser, strADGroup);
				throw _com_error(hr);
			}
		}
	}
	catch(...)
	{
		LogError (_T("failed to add user: %s to group %s"), strUser, strGroup);
		throw;
	}
}

void CUserInstaller::RemoveUserFromGroup(
    const CString& strUser,
    const CString& strUserDomain,
    const CString& strGroup,
    const CString& strGroupDomain)
{
	USES_CONVERSION;
	try
	{
		// Get group domain pointer if possible
		LPCWSTR pwzGroupDomain = NULL;
		if (strGroupDomain.GetLength() > 0)
		{
			pwzGroupDomain = T2CW(strGroupDomain);
		}

		// Try removing it from the global group first
		UINT ui = ::NetGroupDelUser(pwzGroupDomain, T2CW(strGroup), T2CW(strUser));
		if (NERR_GroupNotFound == ui)
		{
			// Build combined domain user name as required.
			CString strLocalUser;
			if (strUserDomain.GetLength() > 0)
			{
				strLocalUser.Format (_T("%s\\%s"), strUserDomain, strUser);
			}
			else
			{
				strLocalUser = strUser;
			}

			// Attempt to add to local group
			LOCALGROUP_MEMBERS_INFO_3 lgmi;
			lgmi.lgrmi3_domainandname = const_cast<LPWSTR>(T2CW(strLocalUser));
			ui = ::NetLocalGroupDelMembers(T2CW(strGroupDomain), T2CW(strGroup),
				3, reinterpret_cast<LPBYTE>(&lgmi), 1);
		}
	    HRESULT hr = HRESULT_FROM_WIN32(ui);
    
		//
		// If we failed, try active directory
		//
		if (FAILED(hr))
		{
			LogWarning (_T("Failed to remove user: %S, domain %S from group: %S, domain: %S with error 0x%x.  Attempting to use Active Directory"),
				strUser, strUserDomain, strGroup, strGroupDomain, hr);

			CString strADUser, strADGroup;
			strADUser.Format (_T("WinNT://%s/%s,user"), strUserDomain, strUser);
			strADGroup.Format (_T("WinNT://%s/%s,group"), strGroupDomain, strGroup);

			IADsGroup *pGroup = NULL;
			hr = ::ADsGetObject(strADGroup.AllocSysString(), IID_IADsGroup, 
				reinterpret_cast<void**>(&pGroup));
			if (FAILED (hr))
			{
				LogError (_T("Failed to get group '%s'."), strADGroup);
				throw _com_error (hr);
			}

			hr = pGroup->Remove(strADUser.AllocSysString());
			if ((HRESULT_FROM_WIN32(ERROR_OBJECT_ALREADY_EXISTS) == hr) || 
				(HRESULT_FROM_WIN32(ERROR_MEMBER_IN_ALIAS) == hr))
			{
				hr = S_OK;
			}
			if (FAILED (hr))
			{
				LogError (_T("Failed to remove user %s to group '%s'."), strADUser, strADGroup);
				throw _com_error(hr);
			}
		}
	}
	catch(...)
	{
		LogError (_T("failed to add user: %s to group %s, continuing..."), strUser, strGroup);
	}
}

bool CUserInstaller::GetUserExists (LPCTSTR pszName, LPCTSTR pszDomain)
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

bool CUserInstaller::IsWellKnownServiceSid(CSid& rSid)
{
	if ((rSid  == Sids::Service()) ||
		(rSid  == Sids::System()) ||
		(rSid  == Sids::NetworkService()))
	{
		// Builtin service account
		return true;
	}

	// It's something else...
	return false;
}

void CUserInstaller::ModifyUserLocalServiceRight(
    const CString& strDomain, const CString& strName, bool fAdd)
{
	try
	{
		USES_CONVERSION;
		HRESULT hr = S_OK;
		NTSTATUS nt = 0;

		CString strFQUserName;
		if (strDomain.GetLength() > 0)
		{
			strFQUserName.Format (_T("%s\\%s"), strDomain, strName);
		}
		else
		{
			strFQUserName = strName;
		}

		// Get the user SID
		CSid sid (strFQUserName);

		// TODO: Test for service account SIDs
		bool fServiceAccount = false;
		fServiceAccount = IsWellKnownServiceSid(sid);

		if (!fServiceAccount)
		{
			LSA_HANDLE hPolicy = NULL;
			LSA_OBJECT_ATTRIBUTES ObjectAttributes = { 0 };
			LSA_UNICODE_STRING lucPrivilege = { 0 };

			nt = ::LsaOpenPolicy(NULL, &ObjectAttributes, POLICY_ALL_ACCESS, &hPolicy);
			hr = HRESULT_FROM_WIN32(::LsaNtStatusToWinError(nt));
			if (FAILED(hr))
			{
				LogError (_T("Failed to open LSA policy store."));
				throw _com_error (hr);
			}
			try
			{
				// Setup for change privilege call
				lucPrivilege.Buffer = L"SeServiceLogonRight";
				lucPrivilege.Length = wcslen(lucPrivilege.Buffer) * sizeof(WCHAR);
				lucPrivilege.MaximumLength = (lucPrivilege.Length + 1) * sizeof(WCHAR);

				// Add or remove as required
				if (fAdd)
				{
					nt = ::LsaAddAccountRights(hPolicy, const_cast<SID*>(sid.GetPSID()), &lucPrivilege, 1);
				}
				else
				{
					nt = ::LsaRemoveAccountRights(hPolicy, const_cast<SID*>(sid.GetPSID()), FALSE, &lucPrivilege, 1);
				}

				// Determine whether error occurred
				hr = HRESULT_FROM_WIN32(::LsaNtStatusToWinError(nt));
				if (FAILED(hr))
				{
					throw _com_error(hr);
				}
			}
			catch(...)
			{
				// Close handle and rethrow
				if (hPolicy)
				{
					::LsaClose(hPolicy);
				}
				throw;
			}

			// Close policy handle
			if (hPolicy)
			{
				::LsaClose(hPolicy);
			}
		}
	}
	catch(...)
	{
		if (fAdd)
		{
			LogError (_T("Failed to grant logon as service rights to user: %s"), strName);
			throw;
		}
		else
		{
			LogError (_T("Failed to remove logon as service right from user: %s, continuing..."), strName);
		}
	}
}

void CUserInstaller::SetUserPasswordAndAttributes(
    USER_INFO_1* pUserInfo, LPWSTR wzPassword, int iAttributes)
{
    // Set the User's password
    pUserInfo->usri1_password = wzPassword;

    // Apply the Attributes
    if (SCAU_DONT_EXPIRE_PASSWRD & iAttributes)
	{
        pUserInfo->usri1_flags |= UF_DONT_EXPIRE_PASSWD;
	}
    else
	{
        pUserInfo->usri1_flags &= ~UF_DONT_EXPIRE_PASSWD;
	}

    if (SCAU_PASSWD_CANT_CHANGE & iAttributes)
	{
        pUserInfo->usri1_flags |= UF_PASSWD_CANT_CHANGE;
	}
    else
	{
        pUserInfo->usri1_flags &= ~UF_PASSWD_CANT_CHANGE;
	}

    if (SCAU_DISABLE_ACCOUNT & iAttributes)
	{
        pUserInfo->usri1_flags |= UF_ACCOUNTDISABLE;
	}
    else
	{
        pUserInfo->usri1_flags &= ~UF_ACCOUNTDISABLE;
	}

    if (SCAU_PASSWD_CHANGE_REQD_ON_LOGIN & iAttributes) // TODO: for some reason this doesn't work
	{
        pUserInfo->usri1_flags |= UF_PASSWORD_EXPIRED;
	}
    else
	{
        pUserInfo->usri1_flags &= ~UF_PASSWORD_EXPIRED;
	}
}
