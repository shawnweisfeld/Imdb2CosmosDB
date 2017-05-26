IMDB in Cosmos Graph
====================

The IMDB database is a great way to learn about Graph Databases, but loading it can be a bit tricky due to its size. This project aims to allow you to download the IMDB source files and load them into a Cosmos Graph database in a few easy steps.

1.	Download the source code
2.	Create some Azure Resources
    +	Resource Group
    +	Storage account
    +	Azure Batch account
    +	Graph Database
        -	NOTE: Place all resources in the same Azure Region if possible
3.	Fill in the configuration values in the app.config file
    +	Mandatory config values
        -	StorageAccountConnectionString
        -	GraphEndpoint
        -	GraphKey
        -	BatchAccountName
        -	BatchAccountKey
        -	BatchAccountUrl
        -   GraphDatabase
    +	Optional config values
        -	All the other config values are optional
4.	Run IT!
5.	Watch the app, download the source file from IMDB, parse them, and then kick off a Azure Batch Job to load it into Cosmos in parallel. 
6.	Clean up the Azure Batch and Storage accounts

