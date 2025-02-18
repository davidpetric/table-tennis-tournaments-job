#!/bin/bash

# Go to project directory
cd /home/table-tennis-tournaments-job

# Load environment variables
set -a
source .env
set +a

# Run the script
/root/.local/state/fnm_multishells/1955_1739916902520/bin/node index.js