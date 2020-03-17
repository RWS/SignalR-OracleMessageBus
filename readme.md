
![Build Badge](https://ci.appveyor.com/api/projects/status/github/sdl/SignalR-OracleMessageBus?svg=true)

SDL SignalR Oracle backplane
=====================


Oracle messaging backplane for scaling out of SignalR applications.

----------


About
-------------

A lot of people are using Oracle as their database of choice, in which case, they cannot just use the same database as a backplane just like you can use SQL Server if that was your database of choice. An option could be just to use REDIS or some other supported bus as backplane, but, that does mean that an additional infrastructure and/or software requirement in order to use the component.

For us as we support both Oracle and SQL Server, we wanted the experience for customers using Oracle the same as customers using SQL Server. Which meant that we needed to create an Oracle backplane.

How To Use
-------------

 1. Get the package Sdl.SignalR.OracleMessageBus from nuget.org.
 2. Before your `app.MapSignalR` call use
`GlobalHost.DependencyResolver.UseOracle("Data Source=ORA12101;User Id=testschema;Password=test123");` Replacing the connection string.
 3. The package will create the necessary tables if they do not exist already.

You can also enable Oracle Dependency if you want to by specifying additional parameter during the `UseOracle` call.

Branches and Contributions
-------------
* master - Represents the latest stable version. This may be a pre-release version
* dev - Represents the current development branch.

License
-------------

Copyright (c) 2017 SDL Group.

Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. You may obtain a copy of the License at

http://www.apache.org/licenses/LICENSE-2.0
Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions and limitations under the License.