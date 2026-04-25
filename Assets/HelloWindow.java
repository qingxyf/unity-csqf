import javax.swing.*;
import java.awt.*;

public class HelloWindow {
    public static void main(String[] args) {
        // 确保在 EDT (Event Dispatch Thread) 中运行 GUI 代码
        SwingUtilities.invokeLater(() -> {
            // 创建窗口
            JFrame frame = new JFrame("你好");
            frame.setDefaultCloseOperation(JFrame.EXIT_ON_CLOSE);
            frame.setSize(300, 200);
            
            // 创建标签
            JLabel label = new JLabel("你好");
            label.setFont(new Font("微软雅黑", Font.PLAIN, 30));
            label.setHorizontalAlignment(SwingConstants.CENTER);
            
            // 添加标签到窗口
            frame.add(label);
            
            // 居中显示窗口
            frame.setLocationRelativeTo(null);
            
            // 显示窗口
            frame.setVisible(true);
        });
    }
} 