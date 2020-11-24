# Setting up SSH on Windows Server 2012

1. (Download)[https://github.com/PowerShell/Win32-OpenSSH/releases/tag/v8.1.0.0p1-Beta] OpenSSH binaries.
1. Extract to C:\Program Files\OpenSSH.
1. In `OpenSSH/`, Run the following in Powershell as admin: `powershell.exe -ExecutionPolicy Bypass -File install-sshd.ps1`.
1. Allow incoming connections to SSH server: `New-NetFirewallRule -Name sshd -DisplayName 'OpenSSH SSH Server' -Enabled True -Direction Inbound -Protocol TCP -Action Allow -LocalPort 22 -Program "C:\Program Files\OpenSSH\sshd.exe"`
1. Enable and start service:
    1. Run services.msc.
    1. Set OpenSSH SSH Server to 'Automatic' startup.
    1. Start service.
1. Get fingerprint of server host key: `Get-ChildItem $env:ProgramData\ssh\ssh_host_*_key | ForEach-Object { . $env:ProgramFiles\OpenSSH\ssh-keygen.exe -l -f $_ }`