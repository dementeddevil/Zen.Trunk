#pragma once
#include "InstallerBase.h"

// url reservation creation attribute definitions
enum SCAUR_ATTRIBUTES
{
	SCAUR_FAIL_IF_EXISTS = 0x00000001,
	SCAUR_UPDATE_IF_EXISTS = 0x00000002,
	SCAUR_DONT_REMOVE_ON_UNINSTALL = 0x00000004,
};

// url reservation acl creation attribute definitions
enum SCAURA_ATTRIBUTES
{
	SCAURA_CAN_REGISTER = 0x00000001,
	SCAURA_CAN_DELEGATE = 0x00000002,
};

class CUrlReservationInstaller : public CInstallerBase
{
public:
	CUrlReservationInstaller (MSIHANDLE handle);

	void ScheduleUrlReservations (WCA_TODO todoScheduled);
	void ExecuteUrlReservation ();

	void AddUrlReservation (LPCTSTR pszUrl, int iAttributes, LPCTSTR pszAcl);
	void RemoveUrlReservation (LPCTSTR pszUrl);

	bool GetUrlReservationExists (LPCTSTR pszUrl);
};
