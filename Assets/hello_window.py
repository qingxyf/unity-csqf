import tkinter as tk
from tkinter import font

def create_window():
    # 创建主窗口
    window = tk.Tk()
    window.title("你好")
    window.geometry("300x200")  # 设置窗口大小

    # 获取屏幕尺寸以计算居中位置
    screen_width = window.winfo_screenwidth()
    screen_height = window.winfo_screenheight()
    
    # 计算窗口位置
    x = (screen_width - 300) // 2
    y = (screen_height - 200) // 2
    
    # 设置窗口位置
    window.geometry(f"300x200+{x}+{y}")

    # 创建标签
    label = tk.Label(
        window,
        text="大笨蛋你被我骗了，这是一个木马程序！！！好吧不骗你了请查看隐藏文件",
        font=("Microsoft YaHei", 30)  # 使用微软雅黑字体
    )
    
    # 将标签放置在窗口中央
    label.place(relx=0.5, rely=0.5, anchor="center")

    # 运行窗口
    window.mainloop()

if __name__ == "__main__":
    create_window() 