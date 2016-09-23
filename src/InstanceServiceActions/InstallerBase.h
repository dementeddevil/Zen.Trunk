#pragma once
#include "InstanceServiceActions.h"
#include "MsiApi.h"

class CInstallerBase
{
public:
	CInstallerBase (MSIHANDLE hInstall);
	~CInstallerBase ();

	CMsiDatabase& GetActiveDatabase ();

	CString GetProperty (LPCTSTR pszPropertyName);
	CString GetFormattedProperty (LPCTSTR pszPropertyName);
	void SetProperty (LPCTSTR pszPropertyName, LPCTSTR pszValue);

	CString GetFormattedString (LPCTSTR pszString);
	CString GetRecordFormattedString (CMsiRecord& record, int iField);

	CString ExtractNextTabDelimitedBlock (CString& strData);
	CString ExtractNextBarDelimitedBlock (CString& strData);

	WCA_TODO GetTranslatedInstallMode (WCA_TODO todoScheduled);
	bool IsRollbackMode ();

	void GetComponentState (LPCTSTR pszComponent, INSTALLSTATE* pInstall, INSTALLSTATE* pAction);
	void SetComponentState (LPCTSTR pszComponent, INSTALLSTATE isState);
	WCA_TODO GetComponentToDo(LPCTSTR pszComponentId);
	bool IsInstalling (INSTALLSTATE isInstalled, INSTALLSTATE isAction);
	bool IsReInstalling(INSTALLSTATE isInstalled, INSTALLSTATE isAction);
	bool IsUninstalling (INSTALLSTATE isInstalled, INSTALLSTATE isAction);

	void DoDeferredAction (LPCTSTR pszCustomAction, LPCTSTR pszCustomActionData, UINT uCost);

	void ProgressMessage (UINT uiCost, bool fExtendProgressBar);

	void LogMsiHandle (LPCTSTR pszType, MSIHANDLE handle);
	void LogInfo (LPCTSTR pszFormat, ...);
	void LogInfoMessage (LPCTSTR pszText);
	void LogUser (LPCTSTR pszFormat, ...);
	void LogUserMessage (LPCTSTR pszText);
	void LogWarning (LPCTSTR pszFormat, ...);
	void LogWarningMessage (LPCTSTR pszText);
	void LogError (LPCTSTR pszFormat, ...);
	void LogErrorMessage (LPCTSTR pszText);
	void LogMessage (INSTALLMESSAGE iMessageType, LPCTSTR pszText);
	UINT ProcessMessage (INSTALLMESSAGE iMessageType, CMsiRecord& rRecord);

private:
	MSIHANDLE m_hInstall;
	CMsiDatabase m_dbActive;
};
