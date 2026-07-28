package com.widdit.nowplaying;

import org.springframework.boot.SpringApplication;
import org.springframework.boot.autoconfigure.SpringBootApplication;
import org.springframework.boot.autoconfigure.jdbc.DataSourceAutoConfiguration;
import org.springframework.boot.autoconfigure.orm.jpa.HibernateJpaAutoConfiguration;
import org.springframework.scheduling.annotation.EnableAsync;
import org.springframework.scheduling.annotation.EnableScheduling;
import org.springframework.web.servlet.config.annotation.EnableWebMvc;

import java.io.BufferedReader;
import java.io.InputStreamReader;

@SpringBootApplication(exclude={DataSourceAutoConfiguration.class, HibernateJpaAutoConfiguration.class})
@EnableAsync
@EnableWebMvc
@EnableScheduling
public class NowPlayingApplication {

    public static void main(String[] args) {
        // 自愈：结束占用 9863 的残留 java 进程（避免上一次关闭窗口导致的端口未释放）
        freeStalePort(9863);
        SpringApplication.run(NowPlayingApplication.class, args);
    }

    /**
     * Windows 上，若上一次启动的 JVM 因直接关闭控制台窗口而残留（仍在监听 port），
     * 会导致本次启动报 "Port ... already in use"。此处在新实例绑定端口前，
     * 主动结束占用该端口的 java/javaw 进程，确保端口可被释放。
     * 仅处理 java 进程，不影响其它占用该端口的服务。
     */
    private static void freeStalePort(int port) {
        if (!System.getProperty("os.name").toLowerCase().contains("win")) {
            return;
        }
        try {
            ProcessBuilder pb = new ProcessBuilder("cmd", "/c",
                    "netstat -ano -p tcp | findstr :" + port);
            pb.redirectErrorStream(true);
            Process p = pb.start();
            try (BufferedReader br = new BufferedReader(new InputStreamReader(p.getInputStream()))) {
                String line;
                while ((line = br.readLine()) != null) {
                    // 例：  TCP    0.0.0.0:9863    0.0.0.0:0    LISTENING    12345
                    String[] parts = line.trim().split("\\s+");
                    if (parts.length >= 5 && "LISTENING".equals(parts[3]) && isJavaProcess(parts[4])) {
                        new ProcessBuilder("taskkill", "/F", "/PID", parts[4]).start();
                        System.out.println("[now-playing] 已结束占用端口 " + port + " 的残留进程 PID=" + parts[4]);
                    }
                }
            }
            p.waitFor();
        } catch (Exception ignored) {
            // 自愈失败不应影响正常启动
        }
    }

    private static boolean isJavaProcess(String pid) {
        try {
            ProcessBuilder pb = new ProcessBuilder("cmd", "/c",
                    "tasklist /FI \"PID eq " + pid + "\" /FO CSV /NH");
            pb.redirectErrorStream(true);
            Process p = pb.start();
            try (BufferedReader br = new BufferedReader(new InputStreamReader(p.getInputStream()))) {
                String line;
                while ((line = br.readLine()) != null) {
                    if (line.contains("java.exe") || line.contains("javaw.exe")) {
                        return true;
                    }
                }
            }
            p.waitFor();
        } catch (Exception ignored) {
        }
        return false;
    }

}
