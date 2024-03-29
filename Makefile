IMG ?= nascarsayan/testpssession:latest

build:
	dotnet build

docker-build:
	docker build . -t ${IMG}

docker-push:
	docker push ${IMG}

docker-run:
	docker run --memory="300m" --memory-swap="300m" --env-file .env --name testpssession -it -d ${IMG}

docker-stop:
	docker rm -f testpssession
