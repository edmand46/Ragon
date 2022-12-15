using NLog;
using Ragon.Common;
using Ragon.Core.Handlers;

namespace Ragon.Core;

public sealed class HandlerRegistry
{
  private IHandler _entityEventHandler;
  private IHandler _entityCreateHandler;
  private IHandler _entityDestroyHandler;
  private IHandler _entityStateHandler;
  private IHandler _sceneLoadedHandler;

  private IHandler _authorizationHandler;
  private IHandler _joinOrCreateHandler;
  private IHandler _createHandler;
  private IHandler _joinHandler;
  private IHandler _leaveHandler;

  private Logger _logger = LogManager.GetCurrentClassLogger();
  private RagonSerializer _reader;
  private RagonSerializer _writer;

  public HandlerRegistry()
  {
    _reader = new RagonSerializer(2048);
    _writer = new RagonSerializer(2048);

    _authorizationHandler = new AuthHandler();
    _joinOrCreateHandler = new JoinOrCreateHandler();
    _sceneLoadedHandler = new SceneLoadedHandler();
    _createHandler = new CreateHandler();
    _joinHandler = new JoinHandler();
    _leaveHandler = new LeaveHandler();

    _entityEventHandler = new EntityEventHandler();
    _entityCreateHandler = new EntityCreateHandler();
    _entityDestroyHandler = new EntityDestroyHandler();
    _entityStateHandler = new EntityStateHandler();
  }

  public void Handle(PlayerContext context, byte[] data)
  {
    _writer.Clear();
    _reader.Clear();
    _reader.FromArray(data);

    var operation = _reader.ReadOperation();
    switch (operation)
    {
      case RagonOperation.REPLICATE_ENTITY_EVENT:
      {
        if (context.RoomPlayer != null)
          _entityEventHandler.Handle(context, _reader, _writer);
        break;
      }
      case RagonOperation.REPLICATE_ENTITY_STATE:
      {
        if (context.RoomPlayer != null)
          _entityStateHandler.Handle(context, _reader, _writer);
        break;
      }
      case RagonOperation.CREATE_ENTITY:
      {
        if (context.RoomPlayer != null)
          _entityCreateHandler.Handle(context, _reader, _writer);
        break;
      }
      case RagonOperation.DESTROY_ENTITY:
      {
        if (context.RoomPlayer != null)
          _entityDestroyHandler.Handle(context, _reader, _writer);
        break;
      }
      case RagonOperation.SCENE_LOADED:
      {
        if (context.RoomPlayer != null)
          _sceneLoadedHandler.Handle(context, _reader, _writer);
        break;
      }
      case RagonOperation.JOIN_OR_CREATE_ROOM:
      {
        _joinOrCreateHandler.Handle(context, _reader, _writer);
        break;
      }
      case RagonOperation.CREATE_ROOM:
      {
        _createHandler.Handle(context, _reader, _writer);
        break;
      }
      case RagonOperation.JOIN_ROOM:
      {
        _joinHandler.Handle(context, _reader, _writer);
        break;
      }
      case RagonOperation.LEAVE_ROOM:
      {
        _leaveHandler.Handle(context, _reader, _writer);
        break;
      }
      case RagonOperation.AUTHORIZE:
      {
        _authorizationHandler.Handle(context, _reader, _writer);
        break;
      }
    }
  }
}