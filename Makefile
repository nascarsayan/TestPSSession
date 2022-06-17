IMG ?= nascarsayan/testpssession:latest

build:
	dotnet build

docker-build:
	docker build . -t ${IMG}

docker-push:
	docker push ${IMG}

docker-run: docker-build
	docker run --name testpssession -it -d -p 5000:80 ${IMG}

docker-stop:
	docker rm -f testpssession
