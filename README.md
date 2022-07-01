## Memory leak in PowerShell remote session in Linux.

1. Clone this repository.
2. Copy `.env.example` to `.env`.
3. Update the hostname, username, and password values to some remote computer.
4a. Running as a docker container:

```sh
docker run --memory="300m" --memory-swap="300m" --env-file .env --name testpssession -it -d nascarsayan/testpssession:latest
```

4b. Running as a kubernetes deployment:

```
kubectl create secret generic pscreds --from-env-file=.env
kubectl create -f deployment.yaml
```
