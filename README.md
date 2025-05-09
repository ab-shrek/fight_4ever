MAP_TYPE=aa_dima2 /Users/mario/fight_4ever/Build/Fight4Ever.app/Contents/MacOS/fight_4ever


# check all game instances running currently
ps aux | grep fight_4ever | grep -v grep

# find all open ports for training server and all clients listening to server
mario@mario fight_4ever % lsof -i -P -n | grep $(ps aux | grep "gpu_training_server.py" | grep -v grep | awk '{print $2}')


To start training, u need these files-
training/run_training.sh
training/requirements.txt
training/gpu_training_server.py
Build/Fight4Ever.app/Contents/MacOS/fights_4ever
