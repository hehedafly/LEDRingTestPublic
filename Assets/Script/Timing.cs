using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;

[System.Serializable]
public struct Timing {
    public string type;//button, dropdown...
    public string name;//pass to ControlsParse
    public int hierarchy;
    [JsonIgnore]
    public float time;//time been set in seconds in unity time
    public string timingMethod;
    public int Id;//unique id for each timingcollection
    public int parentId;
    public string parentName;
    public float value;

    public Timing SetLowerHierarchy() {
        hierarchy--;
        return this;
    }
}

public class TimingCollection{

    public TimingCollection(List<Timing> _timings = null) {
        if (_timings == null) {
            return;
        }
        _timings.Sort((t1, t2) => t1.hierarchy.CompareTo(t2.hierarchy));
        foreach (Timing timing in _timings) {
            timings.Add(timing.Id, timing);
            maxId = Math.Max(maxId, timing.Id);
        }
    }
    
    public TimingCollection(string _json) {
        
        var _timingstr = _json.Split("||JSON_RECORD||");
        _timingstr = _timingstr.Count() == 1? _json.Split("|JR|"): _timingstr;
        try {
            var _timings = _timingstr.Select(s => JsonConvert.DeserializeObject<Timing>(s)).ToList();
            if (_timings is not null) {
                _timings.Sort((t1, t2) => t1.hierarchy.CompareTo(t2.hierarchy));
                foreach (Timing timing in _timings) {
                    timings.Add(timing.Id, timing);
                    maxId = Math.Max(maxId, timing.Id);
                }
            }
        }
        catch {
            timings = new Dictionary<int, Timing>();
        }
    }

    public Timing? this[int hierarchy, string name]{

        get{
            List<Timing> _timings = this.timings.Values.ToList().FindAll(t => t.hierarchy == hierarchy);
            if (_timings.Count == 0){
                return null;
            }
            foreach (Timing timing in _timings){
                if (timing.name == name){
                    return timing;
                }
            }
            return null;
        }

        set{
            if (value.HasValue){
                Remove(name, hierarchy);
            }
            else{
                timings[GetId(hierarchy, name)] = value.Value;
            }
        }
    }

    public TimingCollection this[int hierarchy]{
        get{
            List<Timing> _timings = this.timings.Values.ToList().FindAll(t => t.hierarchy == hierarchy);
            if (_timings.Count == 0){
                return new TimingCollection();
            }
            return new TimingCollection(_timings);
        }
    }

    public TimingCollection this[string name]
    {
        get{
            List<Timing> _timings = this.timings.Values.ToList().FindAll(t => t.name == name);
            if (_timings.Count == 0){
                return null;
            }
            return new TimingCollection(_timings);
        }
    }

    public List<Timing> Times(){
        return timings.Values.ToList();
    }

    public List<int> Keys(){
        return timings.Values.Select(t => t.Id).ToList();
    }

    public Dictionary<int, string> Values() {
        return timings.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.name);
    }

    public bool ContainsKey(string key) {
        return timings.Values.Any(t => t.name == key);
    }

    public List<int> Hierarchies() {
        return timings.Values.Select(t => t.hierarchy).ToList();
    }

    public int GetTimingOrderInSelectHierarchy(int hierarchy, int timingId) {
        if (!timings.ContainsKey(timingId)) { return -1; }
        if(!this[hierarchy].Keys().Contains(timingId)){ return -2; }
        return this[hierarchy].Keys().IndexOf(timingId);
    }
    
    List<Timing> GetTimingsByHierarchyAndName(int hierarchy, string name) {
        int maxHierarchy = hierarchy == -1 ? 999 : hierarchy;
        var res = timings.Values.ToList().Where(t => t.name == name && t.hierarchy >= hierarchy && t.hierarchy <= maxHierarchy).ToList();
        res.Sort((t1, t2) => t1.hierarchy.CompareTo(t2.hierarchy));

        return res;
    }

    public Timing Add(string name, string timingMethod, string type, int hierarchy = 0, int parentId = -1, float time = -1, string parentName = "", float value = -1) {
        int Id = ++maxId;
        if (parentId != -1) {
            Timing? parentTiming = GetTiming(parentId);
            if (parentTiming != null) {
                hierarchy = parentTiming.Value.hierarchy + 1;
            }
        }
        if (time == -1) { time = Time.fixedUnscaledTime; }
        Timing timing = new Timing { type = type, name = name, hierarchy = hierarchy, time = time, timingMethod = timingMethod, Id = Id, parentId = parentId, parentName = parentName, value = value };
        timings.Add(Id, timing);
        return timing;
    }

    public List<Timing> Remove(string name, int hierarchy = -1, bool iterate = false) {
        var selectedTiming = GetTimingsByHierarchyAndName(hierarchy, name);
        if (selectedTiming.Count == 0) { return new List<Timing>(); }
        return Remove(selectedTiming[0].Id, iterate);
    }
    public List<Timing> Remove(int timingId, bool iterate = false) {
        List<int> keysInOrder = timings.Keys.ToList();
        keysInOrder.Sort((t1, t2) => timings[t1].hierarchy.CompareTo(timings[t2].hierarchy));
        List<int> removedId = new List<int>{timingId};
        List<Timing> removedTiming = new List<Timing>();
        List<int> childId = new List<int>();
        foreach (int key in keysInOrder) {
            if (removedId.Contains(key)) {
                removedTiming.Add(timings[key]);
                timings.Remove(key);
            }
            else if (removedId.Contains(timings[key].parentId)) {
                if (iterate) {
                    removedTiming.Add(timings[key]);
                    removedId.Add(timings[key].Id);
                    timings.Remove(key);
                }
                else {
                    childId.Add(key);
                    timings[key] = timings[key].SetLowerHierarchy();
                }
            }
            else if (childId.Contains(timings[key].parentId)) {
                childId.Add(key);
                timings[key] = timings[key].SetLowerHierarchy();
            }
        }
        if (timings.Count() == 0) {
            maxId = 0;
        }
        return removedTiming;
    }

    public List<Timing> Clear() {
        if (timings.Count == 0) { return new List<Timing>(); }
        var res = timings.Values.ToList();
        timings.Clear();
        maxId = 0;
        return res;
    }

    public Timing? GetTiming(int id) {
        if (timings.ContainsKey(id)) {
            return timings[id];
        }
        return null;
    }
    
    public int GetId(int hierarchy, string name){
        var _timings = GetTimingsByHierarchyAndName(hierarchy, name);
        if (_timings.Count == 0){
            return -1;
        }
        return _timings[0].Id;
    }

    public string GetTimingMethod(int hierarchy, string name) {
        var _timings = GetTimingsByHierarchyAndName(hierarchy, name);
        var _timingMethods = _timings.Select(t => t.timingMethod).ToList();
        if (_timingMethods.Count == 0) { return ""; }
        else { return _timingMethods[0].ToString(); }
    }

    public string GetName(int Id) {
        var _t = GetTiming(Id);
        if (!_t.HasValue) { return ""; }
        return _t.Value.name;
    }

    public List<Timing> GetTimingChildren(int parentId) {
        List<Timing> _timings = this.timings.Values.ToList().FindAll(t => t.parentId == parentId);
        return _timings.OrderBy(t => t.Id).ToList();
    }

    public string Export() {
        return string.Join("|JR|",
            timings.Values.Select(t => JsonConvert.SerializeObject(t))
            );
    }

    Dictionary<int, Timing> timings = new Dictionary<int, Timing>();
    int maxId = -1;
    public int Count { get { return timings.Count; } }
}
