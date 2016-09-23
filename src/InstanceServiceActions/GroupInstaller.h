#pragma once
#include "PrincipalInstaller.h"

// group creation attribute definitions
enum SCAG_ATTRIBUTES
{
    SCAG_FAIL_IF_EXISTS = 0x00000010,
    SCAG_UPDATE_IF_EXISTS = 0x00000020,
    SCAG_DONT_REMOVE_ON_UNINSTALL = 0x00000100,
    SCAG_DONT_CREATE_GROUP = 0x00000200,
};

class CGroupInstaller : public CPrincipalInstaller
{
public:
	CGroupInstaller (MSIHANDLE handle);

	void ScheduleGroups (WCA_TODO todoScheduled);
	void ExecuteGroup ();

	void AddGroup (LPCTSTR pszName, LPCTSTR pszDomain, int iAttributes, LPCTSTR pszDescription);
	void RemoveGroup (LPCTSTR pszName, LPCTSTR pszDomain, int iAttributes);

	bool GetGroupExists (LPCTSTR pszName, LPCTSTR pszDomain = NULL);
};
