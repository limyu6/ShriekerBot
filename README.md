# 1. 用Ethernet鏈接電腦和Pi：
## a. 設定pc端的ethernet接口：
設置電腦的IP:\
Settings > Network&Internet > Ethernet > IP assignment > manual > IPV4 > `給電腦的ip`

設置好Device Manager:\
ncpa.cpl > 右鍵Ethernet > Properties > Configure > Power Management > untick "allow the computer ..." > Advanced > EEE/Green Ethernet -> Disabled

在PowerShell中設置防火墻:
```
New-NetFirewallRule -DisplayName "Allow Pi Ping" -Direction Inbound -Protocol ICMPv4 -Action Allow -RemoteAddress 192.168.87.2
```
## b. 設定Pi端的ethernet接口：
取得ethernet的名字:
``` 
nmcli con show
```
設定好該ethernet:
```
sudo nmcli con up "{ethernet的名字}"
sudo nmcli con mod "{ehternet的名字}" ipv4.addresses {給pi的ip}/24 ipv4.method manual
```
## c. 現在電腦可以走ethernet鏈接Pi了。
```
ssh {username}@{pi的ip}
```
# 2. 然後為了避免RPi走ethernet去連internet：
## a.設定default不能走ethernet
```
sudo nmcli con mod "{ethernet的名字}" ipv4.never-default yes
sudo nmcli con up "{ethernet的名字}"
```
## b. 現在RPi又可以正常連公網了。
# 3. 把整個ShriekerBot變成一個system服務，讓它能隨著Pi的啟動自動跑：
## a. 找到{dotnet的路徑}：
```
which dotnet
```
## b. Prepare Service File：
```
sudo nano /etc/systemd/system/shriekerbot.service
```
```
[Unit]
Description=Shrieker Discord Bot Service # 描述：Shrieker Discord 機器人服務
After=network.target network-online.target # 在網路完全啟動後才執行
[Service]
Type=simple # 類型：簡單 (直接執行)
User=admin # 使用者：以 admin 身分執行 (避免用 root 造成權限混亂)
WorkingDirectory=/home/admin/ShriekerBot # 工作目錄：程式的家
ExecStart={dotnet的路徑} run --configuration Release # 啟動指令：用 dotnet run 執行正式版 (Release)
Restart=always # 重啟策略：無論如何都要重啟 (當掉或被殺掉都會復活)
RestartSec=10 # 重啟間隔：死掉後等 10 秒再復活
[Install]
WantedBy=multi-user.target # 安裝目標：在多用戶模式 (標準開機模式) 下啟用
```
Then Ctrl + O -> Enter -> Ctrl + X
## c. Start and Enable：
```
sudo systemctl daemon-reload
sudo systemctl enable shriekerbot
sudo systemctl start shriekerbot
```
## d. Check Status：
```
sudo systemctl status shriekerbot
```
## e. 現在ShriekerBot已經會在開機Pi的時候自動運行了。

# 5. 更新Bot的SOP：
   
## a. Stop the service：
```
sudo systemctl stop shriekerbot
```
## b. Git Pull：
```
cd ~/ShriekerBot
git pull
```
## c. Start the service：
```
sudo systemctl start shriekerbot
```
## d. Check Status：
```
sudo systemctl status shriekerbot
```
# 6. 看Log：
```
sudo journalctl -u shriekerbot -f
```
