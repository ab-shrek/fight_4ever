MAP_TYPE=aa_dima2 /Users/mario/fight_4ever/Build/Fight4Ever.app/Contents/MacOS/fight_4ever


# check all game instances running currently
ps aux | grep "Fight\|fight_4ever\|training\|game" | grep -v grep

# find all open ports for training server and all clients listening to server
lsof -i -P -n | grep $(ps aux | grep "gpu_training_server.py" | grep -v grep | awk '{print $2}')


To start training, u need these files-
training/run_training.sh
training/requirements.txt
training/gpu_training_server.py
Build/Fight4Ever.app/Contents/MacOS/fights_4ever

chmod +x Fight4Ever.x86_64
python run_game_instances.py --instances 10 --duration 120 --cycles 100

# Training Server

A high-performance training server for reinforcement learning with support for GPU acceleration and large-scale data processing.

## System Requirements

- Python 3.10+
- CUDA-capable GPU (optional)
- 200GB RAM recommended
- 40+ CPU cores recommended
- Linux operating system (for optimal performance)

## System Configuration

Before running the server, you need to configure your system limits. Run these commands as root:

```bash
# Add to /etc/sysctl.conf
sudo sh -c 'echo "fs.file-max = 100000" >> /etc/sysctl.conf && echo "net.core.somaxconn = 100000" >> /etc/sysctl.conf && echo "net.ipv4.tcp_max_syn_backlog = 100000" >> /etc/sysctl.conf'

# Apply the new system parameters immediately
sudo sysctl -p

# Add to /etc/security/limits.conf
sudo sh -c 'echo "* soft nofile 100000" >> /etc/security/limits.conf && echo "* hard nofile 100000" >> /etc/security/limits.conf'
```

Note: You must run `sudo sysctl -p` after modifying `/etc/sysctl.conf` for the changes to take effect. A system reboot is not required.

python -m venv venv
source venv/bin/activate
pip install -r requirements.txt



rm server.log; rm -rf ~/.config/unity3d/DefaultCompany/fight_4ever/Player.log; rm -rf logs/*; rm -rf Logs/*; rm -rf checkpoints/*; rm training_server.log; rm -rf trained_models/*; rm -rf weights/*; rm -rf runtime_models/*; rm Assets/Resources/actor_critic*; rm mario.log; rm actor_critic_player*; rm global_model_player*; rm -rf training/build/models/*; rm -rf training/build/logs/*



sudo apt clean
sudo journalctl --vacuum-time=2d
sudo rm -rf ~/.cache/*
sudo rm -rf /var/cache/*
sudo snap remove lxd gnome-42-2204 gtk-common-themes
sudo apt clean
sudo apt autoremove --purge
sudo rm -f /var/lib/snapd/cache/*
sudo swapoff /swap.img
sudo fallocate -l 512M /swap.img
sudo apt remove --purge firefox gnome-shell ubuntu-desktop


sudo apt remove --purge '^nvidia-.*' '^cuda-.*' '^libnvidia-.*' dkms
sudo apt autoremove
sudo apt update
sudo apt install nvidia-driver-535
sudo reboot
nvidia-smi
wget https://developer.download.nvidia.com/compute/cuda/repos/ubuntu2204/x86_64/cuda-ubuntu2204.pin
sudo mv cuda-ubuntu2204.pin /etc/apt/preferences.d/cuda-repository-pin-600
wget https://developer.download.nvidia.com/compute/cuda/12.2.0/local_installers/cuda-repo-ubuntu2204-12-2-local_12.2.0-535.54.03-1_amd64.deb
sudo dpkg -i cuda-repo-ubuntu2204-12-2-local_12.2.0-535.54.03-1_amd64.deb
sudo cp /var/cuda-repo-ubuntu2204-12-2-local/cuda-*-keyring.gpg /usr/share/keyrings/
sudo apt-get update
sudo apt-get -y install cuda



add to ~/.bashrc
export PATH=/usr/local/cuda-12.2/bin:$PATH
export LD_LIBRARY_PATH=/usr/local/cuda-12.2/lib64:$LD_LIBRARY_PATH
source ~/.bashrc


export CGO_CFLAGS="-I/usr/local/cuda-12.2/include"
export CGO_LDFLAGS="-L/usr/local/cuda-12.2/lib64"






sudo apt update
sudo apt install -y golang
go mod tidy
go run gpu_training_server.go



nutanix@nutanix:~/fight_4ever$ sudo nvidia-smi -i 0 --compute-mode 3
[sudo] password for nutanix:
Set compute mode to EXCLUSIVE_PROCESS for GPU 00000000:00:06.0.
All done.
nutanix@nutanix:~/fight_4ever$ nvidia-smi -q | grep Compute
    Conf Compute Protected Memory Usage
    Compute Mode                          : Exclusive_Process
        Compute instance ID               : N/A

        changed from default to exclusive