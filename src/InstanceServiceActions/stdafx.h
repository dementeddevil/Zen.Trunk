#pragma once;

#define WINVER					0x0501
#define _WIN32_WINNT			0x0501
#define _WIN32_WINDOWS			0x0500
#define _WIN32_MSI				310
#define WIN32_LEAN_AND_MEAN 
#define WIN32

#ifndef DEBUG
#undef _ATL_MIN_CRT
#endif

#include <atlbase.h>
#include <atlstr.h>
#include <winerror.h>
#include <msi.h>
#include <msiquery.h>
#include <ntsecapi.h>
#include <atlsecurity.h>
#include <dsgetdc.h>
#include <lm.h>
#include <map>
#include <comdef.h>
#include <iads.h>
#include <adshlp.h>
#include <http.h>
using namespace std;
