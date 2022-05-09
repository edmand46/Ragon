<p align="center">
  <img src="Images/logo.png" width="200" >
</p>

## Ragon Server

Ragon is fully free high perfomance room based game server with plugin based architecture.


<a href="">Documentation</a>
<br>
<a href="">Get started</a>


### Features:
- Free
- Simple matchmaking
- Flexiable API
- Room based architecture
- Extendable room logic via plugin
- Custom authorization
- No CCU limitations* 
- Multi-threaded
- Engine agnostic
- Support any client architecture (MonoBehaviors, ECS)
- UDP

### Roadmap:
- Allow customize matchmaking
- Use native memory 
- Reduce allocations
- Dashboard for monitoring entities and players in realtime
- Statistics for monitoring state of server, cpu, memory
- Docker support
- Add additional API to plugin system

### Dependencies
* ENet-Sharp
* NetStack
* RingBuffer-Unity3D

### License
SSPL-1.0

### Tips
\* Limited to 4095 CCU by library ENet-Sharp
