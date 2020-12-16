# how to run
prepare environment

```
dotnet restore
dotnet tool install Paket --version 6.0.0-beta1
dotnet paket install

```

#start engine(server part)

```
cd DOSP.P4.Server 
dotnet run -- --seed
```

if you want start multiple engine server, you can open a new terminal
and run `dotnet run` without the seed argument

# test the engine (client part)

start a new terminal, run the following command

```
cd DOSP.P4.Client
dotnet run -- 100 
```

you will see some ERROR print, those are tweet or retweet from engine
increase the number of users, the engine will should some unhandled
messages.


#Perfermance

I do not have much time left for testing the client. The only
observation as follow:

## settings 

1 . generate the follower zipf distribution use library
`MathNet.Numerics.Distributions` with parameter a=2.  (in
DOSP.P4.Client.Utils.fs).

2. each client actor have 10% chance to retweet when it receive
a tweet from user it follows.

## observation

1. each user send one tweet, the total retweet would be 50 - 100 % of
number of users.

2. up to 4k users, the engine will show lots of unhandled messages.


#NOTE

This implementation use mongodb as backend database server. the
default mongodb connection is connected to a free Azure Cosmos DB of
my free uf student account, so the number of user without unhandled
message will be < 1k, I suggest test it will 100.

if your can start an local mongodb server, users can be increased to ~
4k. you can start a test mongodb server use docker, and change the
client configure in DOSP.P4.Server.Actors.Common.fs line 50 and 51.
the docker can be started use the following command

`
docker run -it -v mongodata:/data/db -p 27017:27017 --name mongodb -d mongo
`
