#include <windows.h>

LRESULT CALLBACK WindowProc(HWND hwnd, UINT uMsg, WPARAM wParam, LPARAM lParam);

int WINAPI WinMain(HINSTANCE hInstance, HINSTANCE hPrevInstance, LPSTR lpCmdLine, int nCmdShow)
{
    // 注册窗口类
    const wchar_t CLASS_NAME[] = L"Sample Window Class";
    
    WNDCLASS wc = {};
    wc.lpfnWndProc = WindowProc;
    wc.hInstance = hInstance;
    wc.lpszClassName = CLASS_NAME;
    wc.hbrBackground = (HBRUSH)COLOR_WINDOW;
    
    RegisterClass(&wc);

    // 创建窗口
    HWND hwnd = CreateWindowEx(
        0,                          // 扩展窗口样式
        CLASS_NAME,                 // 窗口类名
        L"你好",                    // 窗口标题
        WS_OVERLAPPEDWINDOW,       // 窗口样式
        CW_USEDEFAULT, CW_USEDEFAULT, // 位置
        300, 200,                  // 大小
        NULL,                      // 父窗口句柄
        NULL,                      // 菜单句柄
        hInstance,                 // 实例句柄
        NULL                       // 额外数据
    );

    if (hwnd == NULL)
    {
        return 0;
    }

    ShowWindow(hwnd, nCmdShow);
    UpdateWindow(hwnd);

    // 消息循环
    MSG msg = {};
    while (GetMessage(&msg, NULL, 0, 0))
    {
        TranslateMessage(&msg);
        DispatchMessage(&msg);
    }

    return 0;
}

LRESULT CALLBACK WindowProc(HWND hwnd, UINT uMsg, WPARAM wParam, LPARAM lParam)
{
    switch (uMsg)
    {
    case WM_PAINT:
    {
        PAINTSTRUCT ps;
        HDC hdc = BeginPaint(hwnd, &ps);

        // 设置文本颜色和背景模式
        SetTextColor(hdc, RGB(0, 0, 0));
        SetBkMode(hdc, TRANSPARENT);

        // 创建字体
        HFONT hFont = CreateFont(
            30,                    // 字体高度
            0,                     // 字体宽度
            0,                     // 文本倾斜度
            0,                     // 字符倾斜度
            FW_NORMAL,            // 字体粗细
            FALSE,                // 是否斜体
            FALSE,                // 是否有下划线
            FALSE,                // 是否有删除线
            DEFAULT_CHARSET,      // 字符集
            OUT_DEFAULT_PRECIS,   // 输出精度
            CLIP_DEFAULT_PRECIS,  // 裁剪精度
            DEFAULT_QUALITY,      // 输出质量
            DEFAULT_PITCH | FF_DONTCARE, // 字体间距和字体族
            L"微软雅黑"           // 字体名称
        );

        // 选择新字体
        HFONT hOldFont = (HFONT)SelectObject(hdc, hFont);

        // 获取客户区矩形
        RECT rect;
        GetClientRect(hwnd, &rect);

        // 在窗口中央绘制文本
        DrawText(hdc, L"你好", -1, &rect, DT_SINGLELINE | DT_CENTER | DT_VCENTER);

        // 恢复旧字体并删除新字体
        SelectObject(hdc, hOldFont);
        DeleteObject(hFont);

        EndPaint(hwnd, &ps);
        return 0;
    }
    case WM_DESTROY:
        PostQuitMessage(0);
        return 0;
    }
    return DefWindowProc(hwnd, uMsg, wParam, lParam);
} 