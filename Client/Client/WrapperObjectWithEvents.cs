using System;
using System.Collections.Generic;
using System.Text;

namespace ClientRPC
{

    class EventEmitter
    {

        public event Action<dynamic> subject;


        public void emit(object value)
        {
            this.subject?.Invoke(value);


        }


    }

    class EventItem
    {
      internal  Guid EventKey;
      internal  EventEmitter Event;
     internal   EventItem(Guid EventKey,EventEmitter Event)
        {
            this.EventKey = EventKey;
            this.Event = Event;
        }
}

  public  class WrapperObjectWithEvents
    {
        // словарь имен события и EventKey с EventEmitter
      Dictionary<string, EventItem>  EventsList = new Dictionary<string, EventItem>();

        // Словарь EventKey и EventEmitter
      Dictionary<Guid, EventEmitter>  EventEmittersList = new Dictionary<Guid, EventEmitter>();

        dynamic Target;
        internal TCPClientConnector Connector;
        internal   WrapperObjectWithEvents(dynamic Target, TCPClientConnector Connector)
        {
            this.Target = Target;
            this.Connector = Connector;

    }


// Вызывается при получении внешнего события из .Net
       public  void RaiseEvent(Guid EventKey, object value)
        {
            EventEmitter Event;
            // Если есть подписчики, то вызываем их
            if (EventEmittersList.TryGetValue(EventKey,out Event))
            {
                 Event.emit(value);

            }

        }


        public void AddEventHandler(string EventName, Action<dynamic> eventHandler)
        {

        EventItem ei;
        var isFirst = false;

        if (!this.EventsList.TryGetValue(EventName,out ei)) {

          var EventKey = Guid.NewGuid();

        var Event = new EventEmitter();
        ei = new EventItem(EventKey, Event);
    this.EventsList.Add(EventName, ei);
    this.EventEmittersList.Add(EventKey, Event);
    Connector.EventDictionary.Add(EventKey, this);
        isFirst = true;
        }
        

         ei.Event.subject += eventHandler;


     if (isFirst)
        this.Target.AddEventHandler(ei.EventKey, EventName);

//return res;


}

    public void RemoveEventHandler(string EventName)
    {
            EventItem ei;
            if (this.EventsList.TryGetValue(EventName, out ei))
        {

            var EventKey = ei.EventKey;
            this.Target.RemoveEventHandler(EventKey);
            Connector.EventDictionary.Remove(EventKey);
            this.EventEmittersList.Remove(EventKey);
            this.EventsList.Remove(EventName);
           // ei.Event.Complete();

        }
    }
    public void RemoveAllEventHandler()
    {
      //  this.NetTarget.RemoveAllEventHandler();

        foreach(var ei in EventsList.Values)
        {
         
                Connector.EventDictionary.Remove(ei.EventKey);
                
         }

            this.EventsList.Clear();
            this.EventEmittersList.Clear();
        
    }

    public void Close()
    {
        this.RemoveAllEventHandler();
    //    this.Target(NetObject.FlagDeleteObject);


    }
}

}
