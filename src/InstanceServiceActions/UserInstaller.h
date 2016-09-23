#pragma once
#include "PrincipalInstaller.h"

// user creation attribute definitions
enum SCAU_ATTRIBUTES
{
    SCAU_DONT_EXPIRE_PASSWRD = 0x00000001,
    SCAU_PASSWD_CANT_CHANGE = 0x00000002,
    SCAU_PASSWD_CHANGE_REQD_ON_LOGIN = 0x00000004,
    SCAU_DISABLE_ACCOUNT = 0x00000008,
    SCAU_FAIL_IF_EXISTS = 0x00000010,
    SCAU_UPDATE_IF_EXISTS = 0x00000020,
    SCAU_ALLOW_LOGON_AS_SERVICE = 0x00000040,
	SCAU_IGNORE_SERVICE_ACCOUNTS = 0x00000080,
    SCAU_DONT_REMOVE_ON_UNINSTALL = 0x00000100,
    SCAU_DONT_CREATE_USER = 0x00000200,
};

class CUserInstaller : public CPrincipalInstaller
{
public:
	CUserInstaller (MSIHANDLE handle);

	void ScheduleUsers (WCA_TODO todoScheduled);
	void ExecuteUser ();

	void AddUser (LPCTSTR pszName, LPCTSTR pszDomain, int iAttributes, LPCTSTR pszPassword, LPCTSTR pszMembership);
	void RemoveUser (LPCTSTR pszName, LPCTSTR pszDomain, int iAttributes, LPCTSTR pszMembership);

	void AddUserToGroup(const CString& strUser, const CString& strUserDomain,
		const CString& strGroup, const CString& strGroupDomain);
	void RemoveUserFromGroup(const CString& strUser, const CString& strUserDomain,
		const CString& strGroup, const CString& strGroupDomain);

	bool GetUserExists (LPCTSTR pszName, LPCTSTR pszDomain = NULL);

	bool IsWellKnownServiceSid(CSid& rSid);
	void SetUserPasswordAndAttributes(USER_INFO_1* pUserInfo, 
		LPWSTR wzPassword, int iAttributes);
	void ModifyUserLocalServiceRight(
		const CString& strDomain, const CString& strName, bool fAdd);
};
