<p align="center">
  <img src="Images/ragon-logo.png" width="200" >
</p>

## Ragon Server

Ragon is fully free, small and high perfomance room based game server with plugin based architecture.

<a href="">Documentation</a>
<br>
<a href="">Get started</a>

### Features:
- Effective
- Free
- Simple matchmaking
- Flexiable API
- Room based architecture
- Extendable room logic via plugin
- Custom authorization
- No CCU limitations*
- Engine agnostic
- Support any client architecture (MonoBehaviors, ECS)
- RUDP 

### Roadmap:
- Use native memory 
- Reduce allocations
- Dashboard for monitoring entities and players in realtime
- Statistics for monitoring state of server, cpu, memory
- Horizontal Scaling
- Docker support
- Add additional API to plugin system

### Requirements
- OSX, Windows, Linux(Ubuntu, Debian)
- .NET 6.0

### Dependencies
* ENet-Sharp [v2.4.8]
* NetStack [latest]

### License
SSPL-1.0

### Tips
\* Limited to 4095 CCU by library ENet-Sharp
