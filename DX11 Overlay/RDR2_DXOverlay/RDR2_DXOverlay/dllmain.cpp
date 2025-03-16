#include <Windows.h>
#include <d3d11.h>
#include <dxgi.h>
#include <mutex> // Include the mutex header
#include "imgui.h"
#include "imgui_internal.h"
#include "imconfig.h"
#include "imgui.h"
#include "imgui_impl_dx11.h"
#include "imgui_impl_win32.h"
#include "main.h"
#include "keyboard.h"
#include "natives.h"

typedef HRESULT(__stdcall* Present)(IDXGISwapChain*, UINT, UINT);
Present oPresent;

HWND window = nullptr;
ID3D11Device* pDevice = nullptr;
ID3D11DeviceContext* pContext = nullptr;
ID3D11RenderTargetView* mainRenderTargetView = nullptr;
bool init = false;
std::mutex initMutex;

// Hook for DirectX Present function
HRESULT __stdcall hkPresent(IDXGISwapChain* pSwapChain, UINT SyncInterval, UINT Flags)
{
    OutputDebugStringA("RDR2_DXOverlay: hkPresent() was called!\n");

    if (!pSwapChain)
    {
        OutputDebugStringA("RDR2_DXOverlay: pSwapChain is NULL! Skipping...\n");
        return oPresent(pSwapChain, SyncInterval, Flags);
    }

    if (!init)
    {
        OutputDebugStringA("RDR2_DXOverlay: Initializing DirectX Hook...\n");

        DXGI_SWAP_CHAIN_DESC sd;
        if (FAILED(pSwapChain->GetDesc(&sd)))
        {
            OutputDebugStringA("RDR2_DXOverlay: Failed to get swap chain description!\n");
            return oPresent(pSwapChain, SyncInterval, Flags);
        }

        window = sd.OutputWindow;

        if (SUCCEEDED(pSwapChain->GetDevice(__uuidof(ID3D11Device), (void**)&pDevice)))
        {
            pDevice->GetImmediateContext(&pContext);
            ID3D11Texture2D* pBackBuffer = nullptr;
            if (SUCCEEDED(pSwapChain->GetBuffer(0, __uuidof(ID3D11Texture2D), (LPVOID*)&pBackBuffer)))
            {
                pDevice->CreateRenderTargetView(pBackBuffer, NULL, &mainRenderTargetView);
                pBackBuffer->Release();
            }
        }

        ImGui::CreateContext();
        ImGui_ImplWin32_Init(window);
        ImGui_ImplDX11_Init(pDevice, pContext);
        ImGui_ImplDX11_CreateDeviceObjects();
        init = true;

        OutputDebugStringA("RDR2_DXOverlay: DirectX Hook Initialized Successfully!\n");
    }

    // Start ImGui Frame
    ImGui_ImplDX11_NewFrame();
    ImGui_ImplWin32_NewFrame();
    ImGui::NewFrame();

    // **Force an ImGui Debug Window**
    ImGui::SetNextWindowSize(ImVec2(400, 300));
    ImGui::SetNextWindowPos(ImVec2(640, 360), ImGuiCond_Always);
    ImGui::Begin("Debug Overlay");
    ImGui::Text("Overlay Running!");
    ImGui::End();

    ImGui::Render();
    pContext->OMSetRenderTargets(1, &mainRenderTargetView, NULL);
    ImGui_ImplDX11_RenderDrawData(ImGui::GetDrawData());

    OutputDebugStringA("RDR2_DXOverlay: Render frame complete!\n");

    return oPresent(pSwapChain, SyncInterval, Flags);
}



// DLL entry point
DWORD WINAPI MainThread(LPVOID lpParam)
{
    OutputDebugStringA("RDR2_DXOverlay: MainThread started!\n");

    Sleep(5000); // Wait for game to be fully loaded

    void** pVTable = nullptr;
    IDXGISwapChain* pSwapChain = nullptr;

    // Wait until pSwapChain is valid
    while (!pSwapChain)
    {
        Sleep(100);
    }

    OutputDebugStringA("RDR2_DXOverlay: pSwapChain found!\n");

    pVTable = *reinterpret_cast<void***>(pSwapChain);
    if (pVTable)
    {
        DWORD oldProtect;
        VirtualProtect(&pVTable[8], sizeof(Present), PAGE_EXECUTE_READWRITE, &oldProtect);
        oPresent = (Present)pVTable[8];
        pVTable[8] = reinterpret_cast<void*>(&hkPresent);
        VirtualProtect(&pVTable[8], sizeof(Present), oldProtect, &oldProtect);

        OutputDebugStringA("RDR2_DXOverlay: Hook applied to Present()!\n");
    }
    else
    {
        OutputDebugStringA("RDR2_DXOverlay: Failed to get pVTable!\n");
    }

    return 0;
}



void ScriptMain()
{
    while (true)
    {
        WAIT(0); // Keeps the script running
    }
}

BOOL APIENTRY DllMain(HMODULE hInstance, DWORD reason, LPVOID lpReserved)
{
    switch (reason)
    {
    case DLL_PROCESS_ATTACH:
        scriptRegister(hInstance, ScriptMain); // Registers script with ScriptHookRDR2
        //keyboardHandlerRegister(OnKeyboardMessage); // Optional: Registers a keyboard handler
        break;
    case DLL_PROCESS_DETACH:
        scriptUnregister(hInstance); // Unregisters the script when unloading
        //keyboardHandlerUnregister(OnKeyboardMessage);
        break;
    }
    return TRUE;
}