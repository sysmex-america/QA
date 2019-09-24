/*!
    Sonoma.js Core
    Version: 3.5.0
    Date: 2017-05-02
*/
(function(){!function(a,b){"object"==typeof exports&&"undefined"!=typeof module?b(exports):"function"==typeof define&&define.amd?define(["exports"],b):b(a.RSVP=a.RSVP||{});
}(this,function(a){"use strict";function b(a,b){for(var c=0,d=a.length;c<d;c++)if(a[c]===b)return c;return-1}function c(a){
var b=a._promiseCallbacks;return b||(b=a._promiseCallbacks={}),b}function d(a,b){if("onerror"!==a){if(2!==arguments.length)return va[a];
va[a]=b}else va.on("error",b)}function e(a){return"function"==typeof a||"object"==typeof a&&null!==a}function f(a){return"function"==typeof a;
}function g(a){return"object"==typeof a&&null!==a}function h(){}function i(){setTimeout(function(){for(var a=0;a<Aa.length;a++){
var b=Aa[a],c=b.payload;c.guid=c.key+c.id,c.childGuid=c.key+c.childId,c.error&&(c.stack=c.error.stack),va.trigger(b.name,b.payload);
}Aa.length=0},50)}function j(a,b,c){1===Aa.push({name:a,payload:{key:b._guidKey,id:b._id,eventName:a,detail:b._result,childId:c&&c._id,
label:b._label,timeStamp:ya(),error:va["instrument-with-stack"]?new Error(b._label):null}})&&i()}function k(a,b){var c=this;
if(a&&"object"==typeof a&&a.constructor===c)return a;var d=new c(m,b);return s(d,a),d}function l(){return new TypeError("A promises callback cannot return that same promise.");
}function m(){}function n(a){try{return a.then}catch(b){return Ea.error=b,Ea}}function o(a,b,c,d){try{a.call(b,c,d)}catch(e){
return e}}function p(a,b,c){va.async(function(a){var d=!1,e=o(c,b,function(c){d||(d=!0,b!==c?s(a,c,void 0):u(a,c))},function(b){
d||(d=!0,v(a,b))},"Settle: "+(a._label||" unknown promise"));!d&&e&&(d=!0,v(a,e))},a)}function q(a,b){b._state===Ca?u(a,b._result):b._state===Da?(b._onError=null,
v(a,b._result)):w(b,void 0,function(c){b!==c?s(a,c,void 0):u(a,c)},function(b){return v(a,b)})}function r(a,b,c){b.constructor===a.constructor&&c===C&&a.constructor.resolve===k?q(a,b):c===Ea?(v(a,Ea.error),
Ea.error=null):void 0===c?u(a,b):f(c)?p(a,b,c):u(a,b)}function s(a,b){a===b?u(a,b):e(b)?r(a,b,n(b)):u(a,b)}function t(a){
a._onError&&a._onError(a._result),x(a)}function u(a,b){a._state===Ba&&(a._result=b,a._state=Ca,0===a._subscribers.length?va.instrument&&j("fulfilled",a):va.async(x,a));
}function v(a,b){a._state===Ba&&(a._state=Da,a._result=b,va.async(t,a))}function w(a,b,c,d){var e=a._subscribers,f=e.length;
a._onError=null,e[f]=b,e[f+Ca]=c,e[f+Da]=d,0===f&&a._state&&va.async(x,a)}function x(a){var b=a._subscribers,c=a._state;if(va.instrument&&j(c===Ca?"fulfilled":"rejected",a),
0!==b.length){for(var d=void 0,e=void 0,f=a._result,g=0;g<b.length;g+=3)d=b[g],e=b[g+c],d?A(c,d,e,f):e(f);a._subscribers.length=0;
}}function y(){this.error=null}function z(a,b){try{return a(b)}catch(c){return Fa.error=c,Fa}}function A(a,b,c,d){var e=f(c),g=void 0,h=void 0,i=void 0,j=void 0;
if(e){if(g=z(c,d),g===Fa?(j=!0,h=g.error,g.error=null):i=!0,b===g)return void v(b,l())}else g=d,i=!0;b._state!==Ba||(e&&i?s(b,g):j?v(b,h):a===Ca?u(b,g):a===Da&&v(b,g));
}function B(a,b){var c=!1;try{b(function(b){c||(c=!0,s(a,b))},function(b){c||(c=!0,v(a,b))})}catch(d){v(a,d)}}function C(a,b,c){
var d=arguments,e=this,f=e._state;if(f===Ca&&!a||f===Da&&!b)return va.instrument&&j("chained",e,e),e;e._onError=null;var g=new e.constructor(m,c),h=e._result;
return va.instrument&&j("chained",e,g),f?function(){var a=d[f-1];va.async(function(){return A(f,g,a,h)})}():w(e,g,a,b),g}
function D(a,b,c){return a===Ca?{state:"fulfilled",value:c}:{state:"rejected",reason:c}}function E(a,b,c,d){this._instanceConstructor=a,
this.promise=new a(m,d),this._abortOnReject=c,this._validateInput(b)?(this._input=b,this.length=b.length,this._remaining=b.length,
this._init(),0===this.length?u(this.promise,this._result):(this.length=this.length||0,this._enumerate(),0===this._remaining&&u(this.promise,this._result))):v(this.promise,this._validationError());
}function F(a,b){return new E(this,a,(!0),b).promise}function G(a,b){var c=this,d=new c(m,b);if(!xa(a))return v(d,new TypeError("You must pass an array to race.")),
d;for(var e=0;d._state===Ba&&e<a.length;e++)w(c.resolve(a[e]),void 0,function(a){return s(d,a)},function(a){return v(d,a);
});return d}function H(a,b){var c=this,d=new c(m,b);return v(d,a),d}function I(){throw new TypeError("You must pass a resolver function as the first argument to the promise constructor");
}function J(){throw new TypeError("Failed to construct 'Promise': Please use the 'new' operator, this object constructor cannot be called as a function.");
}function K(a,b){this._id=Ha++,this._label=b,this._state=void 0,this._result=void 0,this._subscribers=[],va.instrument&&j("created",this),
m!==a&&("function"!=typeof a&&I(),this instanceof K?B(this,a):J())}function L(){this.value=void 0}function M(a){try{return a.then;
}catch(b){return Ia.value=b,Ia}}function N(a,b,c){try{a.apply(b,c)}catch(d){return Ia.value=d,Ia}}function O(a,b){for(var c={},d=a.length,e=new Array(d),f=0;f<d;f++)e[f]=a[f];
for(var g=0;g<b.length;g++){var h=b[g];c[h]=e[g+1]}return c}function P(a){for(var b=a.length,c=new Array(b-1),d=1;d<b;d++)c[d-1]=a[d];
return c}function Q(a,b){return{then:function(c,d){return a.call(b,c,d)}}}function R(a,b){var c=function(){for(var c=this,d=arguments.length,e=new Array(d+1),f=!1,g=0;g<d;++g){
var h=arguments[g];if(!f){if(f=U(h),f===Ja){var i=new K(m);return v(i,Ja.value),i}f&&f!==!0&&(h=Q(f,h))}e[g]=h}var j=new K(m);
return e[d]=function(a,c){a?v(j,a):void 0===b?s(j,c):b===!0?s(j,P(arguments)):xa(b)?s(j,O(arguments,b)):s(j,c)},f?T(j,e,a,c):S(j,e,a,c);
};return c.__proto__=a,c}function S(a,b,c,d){var e=N(c,d,b);return e===Ia&&v(a,e.value),a}function T(a,b,c,d){return K.all(b).then(function(b){
var e=N(c,d,b);return e===Ia&&v(a,e.value),a})}function U(a){return!(!a||"object"!=typeof a)&&(a.constructor===K||M(a))}function V(a,b){
return K.all(a,b)}function W(a,b,c){this._superConstructor(a,b,!1,c)}function X(a,b){return new W(K,a,b).promise}function Y(a,b){
return K.race(a,b)}function Z(a,b,c){this._superConstructor(a,b,!0,c)}function $(a,b){return new Z(K,a,b).promise}function _(a,b,c){
this._superConstructor(a,b,!1,c)}function aa(a,b){return new _(K,a,b).promise}function ba(a){throw setTimeout(function(){
throw a}),a}function ca(a){var b={resolve:void 0,reject:void 0};return b.promise=new K(function(a,c){b.resolve=a,b.reject=c;
},a),b}function da(a,b,c){return K.all(a,c).then(function(a){if(!f(b))throw new TypeError("You must pass a function as map's second argument.");
for(var d=a.length,e=new Array(d),g=0;g<d;g++)e[g]=b(a[g]);return K.all(e,c)})}function ea(a,b){return K.resolve(a,b)}function fa(a,b){
return K.reject(a,b)}function ga(a,b){return K.all(a,b)}function ha(a,b){return K.resolve(a,b).then(function(a){return ga(a,b);
})}function ia(a,b,c){var d=xa(a)?ga(a,c):ha(a,c);return d.then(function(a){if(!f(b))throw new TypeError("You must pass a function as filter's second argument.");
for(var d=a.length,e=new Array(d),g=0;g<d;g++)e[g]=b(a[g]);return ga(e,c).then(function(b){for(var c=new Array(d),e=0,f=0;f<d;f++)b[f]&&(c[e]=a[f],
e++);return c.length=e,c})})}function ja(a,b){Ra[Ka]=a,Ra[Ka+1]=b,Ka+=2,2===Ka&&Sa()}function ka(){var a=process.nextTick,b=process.versions.node.match(/^(?:(\d+)\.)?(?:(\d+)\.)?(\*|\d+)$/);
return Array.isArray(b)&&"0"===b[1]&&"10"===b[2]&&(a=setImmediate),function(){return a(pa)}}function la(){return"undefined"!=typeof La?function(){
La(pa)}:oa()}function ma(){var a=0,b=new Oa(pa),c=document.createTextNode("");return b.observe(c,{characterData:!0}),function(){
return c.data=a=++a%2}}function na(){var a=new MessageChannel;return a.port1.onmessage=pa,function(){return a.port2.postMessage(0);
}}function oa(){return function(){return setTimeout(pa,1)}}function pa(){for(var a=0;a<Ka;a+=2){var b=Ra[a],c=Ra[a+1];b(c),
Ra[a]=void 0,Ra[a+1]=void 0}Ka=0}function qa(){try{var a=require,b=a("vertx");return La=b.runOnLoop||b.runOnContext,la()}catch(c){
return oa()}}function ra(a,b,c){return b in a?Object.defineProperty(a,b,{value:c,enumerable:!0,configurable:!0,writable:!0
}):a[b]=c,a}function sa(){va.on.apply(va,arguments)}function ta(){va.off.apply(va,arguments)}var ua={mixin:function(a){return a.on=this.on,
a.off=this.off,a.trigger=this.trigger,a._promiseCallbacks=void 0,a},on:function(a,d){if("function"!=typeof d)throw new TypeError("Callback must be a function");
var e=c(this),f=void 0;f=e[a],f||(f=e[a]=[]),b(f,d)===-1&&f.push(d)},off:function(a,d){var e=c(this),f=void 0,g=void 0;d?(f=e[a],
g=b(f,d),g!==-1&&f.splice(g,1)):e[a]=[]},trigger:function(a,b,d){var e=c(this),f=void 0,g=void 0;if(f=e[a])for(var h=0;h<f.length;h++)(g=f[h])(b,d);
}},va={instrument:!1};ua.mixin(va);var wa=void 0;wa=Array.isArray?Array.isArray:function(a){return"[object Array]"===Object.prototype.toString.call(a);
};var xa=wa,ya=Date.now||function(){return(new Date).getTime()},za=Object.create||function(a){if(arguments.length>1)throw new Error("Second argument not supported");
if("object"!=typeof a)throw new TypeError("Argument must be an object");return h.prototype=a,new h},Aa=[],Ba=void 0,Ca=1,Da=2,Ea=new y,Fa=new y;
E.prototype._validateInput=function(a){return xa(a)},E.prototype._validationError=function(){return new Error("Array Methods must be provided an Array");
},E.prototype._init=function(){this._result=new Array(this.length)},E.prototype._enumerate=function(){for(var a=this.length,b=this.promise,c=this._input,d=0;b._state===Ba&&d<a;d++)this._eachEntry(c[d],d);
},E.prototype._settleMaybeThenable=function(a,b){var c=this._instanceConstructor,d=c.resolve;if(d===k){var e=n(a);if(e===C&&a._state!==Ba)a._onError=null,
this._settledAt(a._state,b,a._result);else if("function"!=typeof e)this._remaining--,this._result[b]=this._makeResult(Ca,b,a);else if(c===K){
var f=new c(m);r(f,a,e),this._willSettleAt(f,b)}else this._willSettleAt(new c(function(b){return b(a)}),b)}else this._willSettleAt(d(a),b);
},E.prototype._eachEntry=function(a,b){g(a)?this._settleMaybeThenable(a,b):(this._remaining--,this._result[b]=this._makeResult(Ca,b,a));
},E.prototype._settledAt=function(a,b,c){var d=this.promise;d._state===Ba&&(this._remaining--,this._abortOnReject&&a===Da?v(d,c):this._result[b]=this._makeResult(a,b,c)),
0===this._remaining&&u(d,this._result)},E.prototype._makeResult=function(a,b,c){return c},E.prototype._willSettleAt=function(a,b){
var c=this;w(a,void 0,function(a){return c._settledAt(Ca,b,a)},function(a){return c._settledAt(Da,b,a)})};var Ga="rsvp_"+ya()+"-",Ha=0;
K.cast=k,K.all=F,K.race=G,K.resolve=k,K.reject=H,K.prototype={constructor:K,_guidKey:Ga,_onError:function(a){var b=this;va.after(function(){
b._onError&&va.trigger("error",a,b._label)})},then:C,"catch":function(a,b){return this.then(void 0,a,b)},"finally":function(a,b){
var c=this,d=c.constructor;return c.then(function(b){return d.resolve(a()).then(function(){return b})},function(b){return d.resolve(a()).then(function(){
throw b})},b)}};var Ia=new L,Ja=new L;W.prototype=za(E.prototype),W.prototype._superConstructor=E,W.prototype._makeResult=D,
W.prototype._validationError=function(){return new Error("allSettled must be called with an array")},Z.prototype=za(E.prototype),
Z.prototype._superConstructor=E,Z.prototype._init=function(){this._result={}},Z.prototype._validateInput=function(a){return a&&"object"==typeof a;
},Z.prototype._validationError=function(){return new Error("Promise.hash must be called with an object")},Z.prototype._enumerate=function(){
var a=this,b=a.promise,c=a._input,d=[];for(var e in c)b._state===Ba&&Object.prototype.hasOwnProperty.call(c,e)&&d.push({position:e,
entry:c[e]});var f=d.length;a._remaining=f;for(var g=void 0,h=0;b._state===Ba&&h<f;h++)g=d[h],a._eachEntry(g.entry,g.position);
},_.prototype=za(Z.prototype),_.prototype._superConstructor=E,_.prototype._makeResult=D,_.prototype._validationError=function(){
return new Error("hashSettled must be called with an object")};var Ka=0,La=void 0,Ma="undefined"!=typeof window?window:void 0,Na=Ma||{},Oa=Na.MutationObserver||Na.WebKitMutationObserver,Pa="undefined"==typeof self&&"undefined"!=typeof process&&"[object process]"==={}.toString.call(process),Qa="undefined"!=typeof Uint8ClampedArray&&"undefined"!=typeof importScripts&&"undefined"!=typeof MessageChannel,Ra=new Array(1e3),Sa=void 0;
Sa=Pa?ka():Oa?ma():Qa?na():void 0===Ma&&"function"==typeof require?qa():oa();var Ta=void 0;if("object"==typeof self)Ta=self;else{
if("object"!=typeof global)throw new Error("no global: `self` or `global` found");Ta=global}var Ua;va.async=ja,va.after=function(a){
return setTimeout(a,0)};var Va=ea,Wa=function(a,b){return va.async(a,b)};if("undefined"!=typeof window&&"object"==typeof window.__PROMISE_INSTRUMENTATION__){
var Xa=window.__PROMISE_INSTRUMENTATION__;d("instrument",!0);for(var Ya in Xa)Xa.hasOwnProperty(Ya)&&sa(Ya,Xa[Ya])}var Za=(Ua={
asap:ja,cast:Va,Promise:K,EventTarget:ua,all:V,allSettled:X,race:Y,hash:$,hashSettled:aa,rethrow:ba,defer:ca,denodeify:R,
configure:d,on:sa,off:ta,resolve:ea,reject:fa,map:da},ra(Ua,"async",Wa),ra(Ua,"filter",ia),Ua);a["default"]=Za,a.asap=ja,
a.cast=Va,a.Promise=K,a.EventTarget=ua,a.all=V,a.allSettled=X,a.race=Y,a.hash=$,a.hashSettled=aa,a.rethrow=ba,a.defer=ca,
a.denodeify=R,a.configure=d,a.on=sa,a.off=ta,a.resolve=ea,a.reject=fa,a.map=da,a.async=Wa,a.filter=ia,Object.defineProperty(a,"__esModule",{
value:!0})});var a=a||function(){function b(a){for(var b in a)a.hasOwnProperty(b)&&(this[b]=a[b]);return this}function c(a){
var b={},c=function(){for(var c=[],d=0;d<arguments.length;d++)c[d]=arguments[d];return c in b||(b[c]=a.apply(this,arguments)),
b[c]};return c}function d(a,b){return a._values=a._values||{},void 0!==a._values[b]?a._values[b]:a._values[b]=a.apply(a,arguments);
}function e(a){return void 0===a||null===a?String(a):x[Object.prototype.toString.call(a)]||"object"}function f(){if(window.Xrm&&Xrm.Page&&Xrm.Page.context&&Xrm.Page.context.client&&Xrm.Page.context.client.getClient)return Xrm.Page.context.client.getClient();
if(!window.GetGlobalContext)return"";var a=window.GetGlobalContext();return a&&a.client&&a.client.getClient?a.client.getClient():void 0;
}function g(){var a,b,c,d,e,f=window.location.host,g="",h=!0,i=!1;if(window.Xrm&&Xrm.Page&&Xrm.Page.context){if(d=Xrm.Page.context,
d.getClientUrl)return Xrm.Page.context.getClientUrl();d.getServerUrl&&(g=Xrm.Page.context.getServerUrl())}else window.GetGlobalContext?(d=window.GetGlobalContext(),
d.getClientUrl?g=Xrm.Page.context.getClientUrl():d.getServerUrl&&(g=Xrm.Page.context.getServerUrl())):(a=unescape(window.location.href).toLowerCase(),
a.indexOf("/webresources")!==-1&&(c=a.split("/webresources")[0],g=c.match(u)[0],h=!1));return g?(e=null!==window.location.protocol.match(w),
i=g.indexOf(window.location.protocol)===-1,h&&!e&&(b=g.match(t)[1],g=g.replace(b,f)),i&&!e&&(g=window.location.protocol+g.substring(g.indexOf(":")+1)),
g.match(v)&&(g=g.substring(0,g.length-1)),g):void alert("Unable to determine server url using Sonoma.getServerUrl.  Please include ClientGlobalContext.js.aspx.");
}function h(){return a.Log.warn(["Deprecation warning. 'Sonoma.getServerUrl' has been deprecated and will be removed in the next major release. ","Please be sure to migrate existing code to use the new 'Sonoma.getClientUrl' function."].join("")),
g()}function i(){var a,b=null;return window.Xrm&&Xrm.Page&&Xrm.Page.context?b=Xrm.Page.context.getOrgUniqueName():window.GetGlobalContext&&(b=GetGlobalContext().getOrgUniqueName()),
b?(a=g().replace(b,""),a.match(v)||(a+="//"),a):void alert("Unable to determine the organization name using Sonoma.getServerUrlWithoutOrg.  Please include ClientGlobalContext.js.aspx.");
}function j(){return a.Log.warn(["Deprecation warning. 'Sonoma.getServerUrlWithoutOrg' has been deprecated and will be removed in the next major release. ","Please be sure to migrate existing code to use the new 'Sonoma.getClientUrlWithoutOrg' function."].join("")),
i()}function k(){var a,b=this;this.length=0,a=["addOnChange","fireOnChange","getAttributeType","getFormat","getInitialValue","getIsDirty","getMax","getMaxLength","getMin","getName","getOption","getParent","getPrecision","getRequiredLevel","getSelectedOption","getSubmitMode","getText","getUserPrivilege","getValue","removeOnChange","setRequiredLevel","setSubmitMode","setValue"],
q(a,function(a,c){b[c]=function(){for(var a=arguments,d=null,e=0;e<this.length;e++){var f=this[e],g=f[c].apply(f,a);g&&null===d&&(d=g);
}return d||b}})}function l(b){var c=new k;return"array"!==a.type(b)&&(b=Array.prototype.slice(arguments,0)),q(b,function(b,d){
var e=r(d);e&&("array"!==a.type(e)&&(e=[e]),q(e,function(){Array.prototype.push.call(c,this)}))}),c}function m(){var a,b=this;
this.length=0,a=["addCustomView","addOption","clearOptions","getAttribute","getControlType","getData","getDefaultView","getDisabled","getLabel","getName","getParent","getSrc","getInitialUrl","getObject","getVisible","refresh","removeOption","setData","setDefaultView","setDisabled","setFocus","setLabel","setSrc","setVisible"],
q(a,function(a,c){b[c]=function(){for(var a=arguments,d=null,e=0;e<this.length;e++){var f=this[e];if(f[c]){var g=f[c].apply(f,a);
g&&null===d&&(d=g)}}return d||b}})}function n(b){var c=new m;return"array"!==a.type(b)&&(b=Array.prototype.slice.call(arguments,0)),
q(b,function(b,d){var e=s(d);e&&("array"!==a.type(e)&&(e=[e]),q(e,function(){Array.prototype.push.call(c,this)}))}),c}function o(){
var a,b=Array.prototype.slice.call(arguments,0);return 0===b.length||(a=b[0],q(b,function(b,c){var d=c===a;return a=c,d}));
}function p(a){for(var b,c,d=(a||window.location.search||"").replace(/^\?/,""),e={},f=d.split("&"),g=f.length;g--;)b=f[g].split("="),
2===b.length&&(c=unescape(b[1]),/(.+?)=/.test(c)&&(c=p(c)),e[b[0].toLowerCase()]=c);return e}function q(b,c){var d,e;if("function"===a.type(c))switch(a.type(b)){
case"array":for(d=0;d<b.length;d++)if(c.call(b[d],d,b[d])===!1)return!1;return!0;case"object":for(e in b)if(b.hasOwnProperty(e)&&c.call(b[e],e,b[e])===!1)return!1;
return!0;default:throw new Error("Sonoma.each does not support the object "+b.toString())}}for(var r,s,t=/^(?:http)(?:s)?\:\/\/([^\/]+)/i,u=/[^{]*/,v=/\/$/,w=/^about:/,x={},y="Boolean Number String Function Array Date RegExp Object".split(" "),z=0,A=y.length;z<A;z++)x["[object "+y[z]+"]"]=y[z].toLowerCase();
return r=c(function(a){if(window.Xrm&&Xrm.Page&&Xrm.Page.getAttribute)return Xrm.Page.getAttribute(a);throw new Error("Cannot use getAttribute: Xrm.Page.getAttribute is not available.");
}),s=c(function(a){if(window.Xrm&&Xrm.Page&&Xrm.Page.getControl)return Xrm.Page.getControl(a);throw"Cannot use getControl: Xrm.Page.getControl is not available.";
}),{areEqual:o,each:q,getClient:f,getClientUrl:g,getServerUrl:h,getServerUrlWithoutOrg:j,getQueryStringParams:p,getAttribute:l,
getControl:n,extend:b,memoize:c,memoized:d,type:e,version:"3.5.0"}}();a.Array=function(){function a(a,c,d){var e,f;if(!a||!b(a))return-1;
if(Array.prototype.indexOf)return Array.prototype.indexOf.call(a,c,d);for(e=(d||0)-1,f=a.length;++e<f;)if(a[e]===c)return e;
return-1}function b(a){return Array.isArray?Array.isArray(a):"[object Array]"===Object.prototype.toString.call(a)}return{
indexOf:a,isArray:b}}(),a.String=function(){function a(a){var b,c,d,e=0,f=[];if(!a)return"";if(f=Array.prototype.slice.call(arguments,1),
c=a.match(/\{\d+\}/g))for(b=c.length;e<b;e++)d=parseInt(c[e].match(/\d+/),10),f.length>d&&(a=a.replace(c[e],f[d]));return a;
}function b(a){return a.replace(g,"")}function c(){var a=Array.prototype.slice.call(arguments,0);return a.join("")}function d(a){
return""===a||void 0===a||null===a||"string"!=typeof a}function e(a,b,c){for(var d=[];c-- >0;)d.push(b);return d.push(a),
d.join("")}function f(a,b,c){for(var d=[a];c-- >0;)d.push(b);return d.join("")}var g=/^\s+|\s+$/g;return{format:a,trim:b,
concat:c,isNullOrEmpty:d,padLeft:e,padRight:f}}(),a.Date=function(){function b(a,b){var c,d,e,f,g,h,i=a.toString();return i=b.replace(/yyyy/g,a.getFullYear()),
i=i.replace(/yy/g,(a.getFullYear()+"").substring(2)),c=a.getMonth(),i=i.replace(/MM/g,c+1<10?"0"+(c+1):c+1),i=i.replace(/(\\)?M/g,function(a,b){
return b?a:c+1}),d=a.getDate(),i=i.replace(/dd/g,d<10?"0"+d:d),i=i.replace(/(\\)?d/g,function(a,b){return b?a:d}),e=a.getHours(),
f=e>12?e-12:e,i=i.replace(/HH/g,e<10?"0"+e:e),i=i.replace(/(\\)?H/g,function(a,b){return b?a:e}),i=i.replace(/hh/g,e<10?"0"+f:f),
i=i.replace(/(\\)?h/g,function(a,b){return b?a:f}),g=a.getMinutes(),i=i.replace(/mm/g,g<10?"0"+g:g),i=i.replace(/(\\)?m/g,function(a,b){
return b?a:g}),h=a.getSeconds(),i=i.replace(/ss/g,h<10?"0"+h:h),i=i.replace(/(\\)?s/g,function(a,b){return b?a:h}),i=i.replace(/fff/g,a.getMilliseconds()),
i=i.replace(/tt/g,a.getHours()>=12?j.PMDesignator:j.AMDesignator),i.replace(/\\/g,"")}function c(a){var b,c,d,e,f,g,h,i,j,l,m,n=Date.parse(a),o=0;
return b=k.exec(a),isNaN(n)&&b&&(c=parseInt(b[1],10)||0,d=(parseInt(b[2],10)||0)-1,e=parseInt(b[3],10)||0,f=parseInt(b[4],10)||0,
g=parseInt(b[5],10)||0,h=parseInt(b[6],10)||0,i=parseInt(b[7],10)||0,l=parseInt(b[10],10)||0,m=parseInt(b[11],10)||0,"Z"!==b[8]&&(j=60*l+m,
"+"===b[9]&&(o=0-o)),n=Date.UTC(c,d,e,f,g+j,h,i)),n}function d(b){var c,d,e,f,g,h;return b instanceof Date?(c=b.getUTCMonth()+1,
1===c.toString().length&&(c=a.String.padLeft(c,"0",1)),d=b.getUTCDate(),1===d.toString().length&&(d=a.String.padLeft(d,"0",1)),
e=b.getUTCHours(),1===e.toString().length&&(e=a.String.padLeft(e,"0",1)),f=b.getUTCMinutes(),1===f.toString().length&&(f=a.String.padLeft(f,"0",1)),
g=b.getUTCSeconds(),1===g.toString().length&&(g=a.String.padLeft(g,"0",1)),h=b.getUTCMilliseconds(),1===h.toString().length?h=a.String.padLeft(h,"0",2):2===h.toString().length&&(h=a.String.padLeft(h,"0",1)),
b.getUTCFullYear()+"-"+c+"-"+d+"T"+e+":"+f+":"+g+"."+h+"Z"):void a.Log.error("Error in Sonoma.Date.toISOString: Object of type Date was expected.");
}function e(b){switch(a.type(b)){case"date":return b;case"array":return new Date(b[0],b[1],b[2]);case"number":return new Date(b);
case"string":return new Date(c(b));case"object":if(void 0!==b.year&&void 0!==b.month&&void 0!==b.date)return new Date(b.year,b.month,b.date);
}return NaN}function f(a,b,c){return isFinite(a=e(a).valueOf())&&isFinite(b=e(b).valueOf())&&isFinite(c=e(c).valueOf())?b<=a&&a<=c:NaN;
}function g(a){var b=e(a);return b.setHours(0),b.setMinutes(0),b.setSeconds(0),b.setMilliseconds(0),b}function h(a){var c=e(a);
return b(c,j.ShortDatePattern)}function i(a){var c=e(a);return b(c,j.ShortTimePattern)}var j={ShortDatePattern:"M/d/yyyy",
ShortTimePattern:"h:mm tt",AMDesignator:"AM",PMDesignator:"PM"},k=/(\d{4})-?(\d{2})-?(\d{2})(?:[T ](\d{2}):?(\d{2}):?(\d{2})?(?:\.(\d{1,3})[\d]*)?(?:(Z)|([+\-])(\d{2})(?::?(\d{2}))?))/;
try{j=Sys.CultureInfo.CurrentCulture.dateTimeFormat}catch(l){}return{parse:c,toISOString:d,convert:e,inRange:f,zeroTime:g,
getShortDateFormat:h,getShortDateTimeFormat:i}}(),a.Guid=function(){function b(a){return a.replace(j,"")}function c(a){return"{"+b(a)+"}";
}function d(b){return"string"===a.type(b)&&(b=f(b),k.test(b))}function e(b){if(!d(b))return null;b=f(b);var c=l.exec(b);return b=a.String.format("{{0}-{1}-{2}-{3}-{4}}",c[1],c[2],c[3],c[4],c[5]);
}function f(b){return"string"!==a.type(b)?null:b.replace(m,"").toUpperCase()}function g(){var b=[],c=Array.prototype.slice.call(arguments,0);
return a.each(c,function(){b.push(f(this))}),a.areEqual.apply(null,b)}function h(a){switch(a=a||"db",a.toLowerCase()){case"n":
return n;case"b":return p;case"db":case"bd":return q;case"d":return o;default:return q}}function i(a){return g(a,h())}var j=/[{}]/g,k=/^([0-9A-F]{32})$/,l=/^([0-9A-F]{8})([0-9A-F]{4})([0-9A-F]{4})([0-9A-F]{4})([0-9A-F]{12})$/,m=/[\s{}\-]/g,n="00000000000000000000000000000000",o="00000000-0000-0000-0000-000000000000",p="{00000000000000000000000000000000}",q="{00000000-0000-0000-0000-000000000000}";
return{decapsulate:b,encapsulate:c,isValid:d,isEmpty:i,format:e,areEqual:g,empty:h}}(),a.Cache=function(){function a(a){var b,c,e=0;
for(b=d.length;e<b;e++)if(d[e].name===a)return d[e];return c={name:a,data:[]},d.push(c),c}function b(b,c){var d,e=a(b),f=0;
for(d=e.data.length;f<d;f++)if(e.data[f].key===c)return e.data[f].value;return null}function c(b,c,d){var e,f=a(b),g=0;for(e=f.data.length;g<e;g++)if(f.data[g].key===c)return void(f.data[g].value=d);
f.data.push({key:c,value:d})}var d=[];return{get:b,set:c}}(),a.LocalStorage=function(){function a(a){var b,c=null,d=window.localStorage.getItem(a);
return d&&(d=JSON.parse(d),b=d.expiration||0,b&&b<=(new Date).valueOf()?window.localStorage.removeItem(a):c=d.value),c}function b(a,b,c){
var e;null===b?window.localStorage.removeItem(a):(b={value:b,expiration:null},c&&(e=new Date((new Date).valueOf()+d*c),b.expiration=e.valueOf()),
window.localStorage.setItem(a,JSON.stringify(b)))}function c(a){window.localStorage.removeItem(a)}var d=36e5;return window.localStorage||(window.localStorage={
getItem:function(){return null},setItem:function(){},removeItem:function(){}}),{set:b,get:a,remove:c}}(),a.Log=function(){
function a(){window.console&&console.log&&Function.prototype.apply.call(console.log,console,arguments),o.push.apply(o,arguments);
}function b(){window.console&&console.info?Function.prototype.apply.call(console.info,console,arguments):window.console&&console.log&&Function.prototype.apply.call(console.log,console,arguments),
o.push.apply(o,arguments)}function c(){window.console&&console.warn?Function.prototype.apply.call(console.warn,console,arguments):window.console&&console.log&&Function.prototype.apply.call(console.log,console,arguments),
o.push.apply(o,arguments)}function d(){window.console&&console.error?Function.prototype.apply.call(console.error,console,arguments):window.console&&console.log&&Function.prototype.apply.call(console.log,console,arguments),
o.push.apply(o,arguments)}function e(){return o}function f(){o=[]}function g(){window.console&&console.group?console.group():window.console&&console.log&&console.log("+++");
}function h(){window.console&&console.groupEnd?console.groupEnd():window.console&&console.log&&console.log("---")}function i(){
window.console&&console.groupCollapsed?console.groupCollapsed():g()}function j(a){a||(a="default"),window.console&&console.time?console.time(a):p[a]=l();
}function k(b){b||(b="default"),window.console&&console.timeEnd?console.timeEnd(b):p[b]&&(a(b+": "+(l()-p[b])+"ms"),delete p[b]);
}function l(){return Date.now?Date.now():(new Date).getTime()}function m(a){window.console&&console.dir?console.dir(a):window.console&&console.log&&JSON&&JSON.stringify&&console.log(JSON.stringify(a));
}function n(){window.console&&console.trace?console.trace():window.console&&console.error?console.error("Stracktrace"):window.console&&console.log&&console.log("console.trace not available on this browser.");
}var o=[],p={};return{log:a,debug:a,info:b,warn:c,error:d,getLogs:e,clearLogs:f,group:g,groupEnd:h,groupCollapsed:i,time:j,
timeEnd:k,dir:m,trace:n}}(),a.Xml=function(b){"use strict";function c(b){return"array"===a.type(b)&&(b=b.join("")),"string"!==a.type(b)&&(b=b.toString()),
b.replace(/&/g,"&amp;").replace(/</g,"&lt;").replace(/>/g,"&gt;").replace(/"/g,"&quot;").replace(/'/g,"&#39;")}function d(c){
var d;if(!c||"string"!=typeof c)return null;try{d=(new window.DOMParser).parseFromString(c,"text/xml")}catch(e){d=b}return d&&!d.getElementsByTagName("parsererror").length||a.Log.error("Invalid XML: "+c),
d}function e(a){if(!(this instanceof e))return new e(a);if("undefined"==typeof a)throw new Error("tagName is required.");this.tagName=a,
this.attributes={},this.elements=[]}return e.prototype=function(){function a(a,b){if("undefined"==typeof a)throw new Error("key is required.");
this.attributes[a]=c(b.toString())}function b(a){if("undefined"==typeof a)throw new Error("element is required.");if(a instanceof Array)for(var b=0,c=a.length;b<c;++b)this.elements.push(a[b]);else this.elements.push(a);
}function d(a){for(var b=[];a--;)b.push("\t");return b.join("")}function f(a,b){var f,g,h,i,j,k="undefined"!=typeof a&&a,l=[];
b=b||0,f=d(b++),k&&l.push(f),l.push("<"+this.tagName);for(g in this.attributes)this.attributes.hasOwnProperty(g)&&l.push(" "+g+"='"+this.attributes[g]+"'");
if(0===this.elements.length)l.push(" />");else{for(l.push(">"),h=0,i=this.elements.length;h<i;++h)j=this.elements[h],j instanceof e?(k&&l.push("\n"),
l.push(j.toString(k,b))):(k&&l.push("\n"+d(b)),l.push(c(j.toString())));k&&l.push("\n"+f),l.push("</"+this.tagName+">")}return l.join("");
}return{addAttribute:a,addElement:b,toString:f}}(),{XmlObject:e,xmlEncode:c,loadXml:d}}(),function(a,b){b.Promise.prototype.done=function(){
return a.Log.warn(["Deprecation warning: 'done' will be removed in the next major release. ","Please switch to using 'then' as outlined in the Promises/A+ specification."].join("")),
b.Promise.prototype.then.apply(this,arguments)},b.Promise.prototype.fail=function(){return a.Log.warn(["Deprecation warning: 'fail' will be removed in the next major release. ","Please switch to using 'catch' (or 'caught') as outlined in the Promises/A+ specification."].join("")),
b.Promise.prototype["catch"].apply(this,arguments)},a.Promise=b,a.Promise.resolve=function(){var b=arguments;return new a.Promise.Promise(function(a,c){
a.apply(this,b)})},a.Promise.reject=function(){var b=arguments;return new a.Promise.Promise(function(a,c){c.apply(this,b);
})}}(a,RSVP),a.Fetch=function(){"use strict";function b(b,c,d,e){if("undefined"==typeof b)throw new Error("name is required.");
var f=new a.Xml.XmlObject("attribute");return f.addAttribute("name",b),c&&f.addAttribute("aggregate","countcolumn"),d&&f.addAttribute("alias",d),
e&&f.addAttribute("distinct",e),f}function c(b,c){if("undefined"==typeof b)throw new Error("attribute is required.");var d=new a.Xml.XmlObject("order");
return d.addAttribute("attribute",b),d.addAttribute("descending",c||!1),d}function d(b,c,e,f){var g,h;if(!(this instanceof d))return new d(b,c,e,f);
if("undefined"==typeof b)throw new Error("entityName is required.");g=new a.Xml.XmlObject("fetch"),g.addAttribute("mapping","logical"),
c&&g.addAttribute("count",c),this.isAggregate="undefined"!=typeof e&&e,this.isAggregate&&g.addAttribute("aggregate",this.isAggregate),
this.isDistinct="undefined"!=typeof f&&f,this.isDistinct&&g.addAttribute("distinct",this.isDistinct),g.addAttribute("version","1.0"),
h=new a.Xml.XmlObject("entity"),h.addAttribute("name",b),g.addElement(h),this.rootXml=g,this.entityXml=h,this.filterAdded=!1;
}function e(b){if(!(this instanceof e))return new e(b);var c=new a.Xml.XmlObject("filter");b&&c.addAttribute("type","or"),
this.rootXml=c}function f(b,c,d,e,g){if(!(this instanceof f))return new f(b,c,d,g,e);if("undefined"==typeof b)throw new Error("toEntity is required.");
if("undefined"==typeof c)throw new Error("fromAttribute is required.");if("undefined"==typeof d)throw new Error("toAttribute is required.");
var h=new a.Xml.XmlObject("link-entity");h.addAttribute("name",b),h.addAttribute("from",c),h.addAttribute("to",d),g&&h.addAttribute("alias",g),
e&&h.addAttribute("link-type",e),this.rootXml=h,this.filterAdded=!1}var g={isBetween:"between",isEqualTo:"eq",isEqualToBusinessUnitId:"eq-businessid",
isEqualToUserId:"eq-userid",isGreaterThan:"gt",isGreaterThanOrEqualTo:"ge",isIn:"in",isLessThan:"lt",isLessThanOrEqualTo:"le",
isLike:"like",isNotBetween:"not-between",isNotEqualTo:"ne",isNotEqualToBusinessUnitId:"ne-businessid",isNotEqualToUserId:"ne-userid",
isNotIn:"not-in",isNotLike:"not-like",isNotNull:"not-null",isNull:"null",isOn:"on",isOnOrAfter:"on-or-after",isOnOrBefore:"on-or-before",
isWithinLast7Days:"last-seven-days",isWithinLastMonth:"last-month",isWithinLastXDays:"last-x-days",isWithinLastXHours:"last-x-hours",
isWithinLastXMonths:"last-x-months",isWithinLastXWeeks:"last-x-weeks",isWithinLastXYears:"last-x-years",isWithinLastYear:"last-year",
isWithinNext7Days:"next-seven-days",isWithinNextMonth:"next-month",isWithinNextWeek:"next-week",isWithinNextXDays:"next-x-days",
isWithinNextXHours:"next-x-hours",isWithinNextXMonths:"next-x-months",isWithinNextXWeeks:"next-x-weeks",isWithinNextXYears:"next-x-years",
isWithinNextYear:"next-year",isWithinThisMonth:"this-month",isWithinThisWeek:"this-week",isWithinThisYear:"this-year",isWithinToday:"today",
isWithinTomorrow:"tomorrow",isWithinYesterday:"yesterday"},h={inner:"inner",natural:"natural",outer:"outer"};return d.prototype=function(){
function a(a,c,d){var e=b(a,this.isAggregate,c,d);return this.entityXml.addElement(e),this}function d(a,b){var d=c(a,b);return this.entityXml.addElement(d),
this}function g(a){if(!(a instanceof e))throw new Error("filter must be a Filter type.");if(this.filterAdded)throw new Error("Only one filter may be added per fetch. Filters can be nested inside each other.");
return this.entityXml.addElement(a.rootXml),this.filterAdded=!0,this}function h(a){if(!(a instanceof f))throw new Error("linkEntity must be a LinkEntity type.");
return this.entityXml.addElement(a.rootXml),this}function i(a){return this.rootXml.toString(a)}return{addAttribute:a,addOrder:d,
addFilter:g,addLinkEntity:h,toString:i}}(),e.prototype=function(){function b(b,c,d){var e,f,g=0;if("undefined"==typeof b)throw new Error("attribute is required.");
if("undefined"==typeof c)throw new Error("operator is required.");if(e=new a.Xml.XmlObject("condition"),e.addAttribute("attribute",b),
e.addAttribute("operator",c),d instanceof Array)for(g;g<d.length;++g)f=new a.Xml.XmlObject("value"),f.addElement(d[g]),e.addElement(f);else d&&e.addAttribute("value",d);
return this.rootXml.addElement(e),this}function c(a){if(!(a instanceof e))throw new Error("filter must be a Filter type.");
return this.rootXml.addElement(a.rootXml),this}return{addCondition:b,addFilter:c}}(),f.prototype=function(){function a(a){
var c=b(a);return this.rootXml.addElement(c),this}function d(a,b){var d=c(a,b);return this.rootXml.addElement(d),this}function g(a){
if(!(a instanceof e))throw new Error("filter must be a Filter type.");if(this.filterAdded)throw new Error("Only one filter may be added per fetch. Filters can be nested inside each other.");
return this.rootXml.addElement(a.rootXml),this.filterAdded=!0,this}function h(a){if(!(a instanceof f))throw new Error("linkEntity must be a LinkEntity type.");
return this.rootXml.addElement(a.rootXml),this}return{addAttribute:a,addOrder:d,addFilter:g,addLinkEntity:h}}(),{FetchXml:d,
Filter:e,LinkEntity:f,Operators:g,JoinTypes:h}}(),a.QueryBuilder=function(){"use strict";function b(){this.entityName=null,
this.attributes=[],this.orders=[],this.filters=[],this.joins=[]}function c(a,b,c){this.attribute=a,this.operator=b,this.values=c;
}return b.prototype=function(){function b(a){if("string"!=typeof a)throw new Error("entityName must be a string.");return this.entityName=a,
this}function d(a){if(a instanceof Array)for(var b=0;b<a.length;++b)this.attributes.push(a[b]);else"string"==typeof a&&this.attributes.push(a);
return this}function e(){return this.attributes=[],this}function f(a,b,c,d,e,f){for(var g={toEntity:a,fromAttribute:b,toAttribute:c,
joinType:d,alias:f},h=[],i=0;i<e.length;++i)h.push(e[i]);return g.filters={conditions:h},this.joins.push(g),this}function g(a,b,d){
return this.filters.push({conditions:[new c(a,b,d)]}),this}function h(a,b){var c={};return c.attribute=a,c.isDescending=b,
this.orders.push(c),this}function i(b,c){var d=new a.Fetch.LinkEntity(c.toEntity,c.fromAttribute,c.toAttribute,c.joinType,c.alias),e=j(c.filters);
d.addFilter(e),b.addLinkEntity(d)}function j(b){for(var c,d=new a.Fetch.Filter(b.isOr),e=0;e<b.conditions.length;++e)c=b.conditions[e],
d.addCondition(c.attribute,c.operator,c.values);return d}function k(){var b,c=new a.Fetch.FetchXml(this.entityName),d=0;for(d=0;d<this.attributes.length;++d)c.addAttribute(this.attributes[d]);
for(d=0;d<this.orders.length;++d)c.addOrder(this.orders[d].attribute,this.orders[d].isDescending);for(d=0;d<this.filters.length;++d)b=j(this.filters[d]),
c.addFilter(b);for(d=0;d<this.joins.length;++d)i(c,this.joins[d]);return c.toString()}return{From:b,Select:d,SelectAll:e,
Join:f,Order:h,toString:k,Where:g}}(),a.QB=a.QueryBuilder,{Query:b,Condition:c}}(),a.OrgService=function(){"use strict";function b(b){
var c,d,e,f;if(c="array"!==a.type(b),d=["<columnSet ",Ga.xrm,">","<a:AllColumns>",c.toString(),"</a:AllColumns>","<a:Columns ",Ga.arrays,">"],
!c)for(f=b.length,e=0;e<f;e++)"string"===a.type(b[e])&&d.push(a.String.format("<b:string>{0}</b:string>",b[e]));return d.push("</a:Columns>"),
d.push("</columnSet>"),d.join("")}function c(a,b){var c,d={};for(c in a)a.hasOwnProperty(c)&&(d[c]=a[c]);for(c in b)b.hasOwnProperty(c)&&(d[c]=b[c]);
return d}function d(a){return"undefined"==typeof a}function e(a){return null===a}function f(b){var c,d,e;c=[];for(d in b){
if("Metadata"===d||!b.hasOwnProperty(d))return;if(c.push("<a:KeyValuePairOfstringanyType>"),c.push(a.String.format("<b:key>{0}</b:key>",d)),
e=b[d],"undefined"===a.type(e)||null===e)return void alert("To set an attribute to null, set its value to a new instance of the OrgService.NullValue class.");
switch(a.type(e)){case"string":c.push('<b:value i:type="c:string" '+Ga.xml+">"+a.Xml.xmlEncode(b[d])+"</b:value>");break;case"number":
c.push('<b:value i:type="c:int" '+Ga.xml+">"+b[d]+"</b:value>");break;default:e instanceof va==!1&&alert("The attribute "+d+" is a complex type, but can not be serialized.  Make sure you define the attribute using the appropriate class (new Sonoma.OrgService.[Boolean|DateTime|Decimal|EntityReference|Guid|OptionSetValue])."),
c.push(e.toXml())}c.push("</a:KeyValuePairOfstringanyType>")}return c.join("")}function g(b,c,d){if("object"!==a.type(d))return void alert("Entity is not an object, cannot continue operation.");
if(e(b))return void alert("LogicalName was not specified, cannot continue operation.");e(c)&&(c="00000000-0000-0000-0000-000000000000");
var g=["<a:Attributes ",Ga.collection,">",f(d),"</a:Attributes>",'<a:EntityState i:nil="true" />',"<a:FormattedValues ",Ga.collection," />","<a:Id>"+c+"</a:Id>","<a:LogicalName>"+b+"</a:LogicalName>","<a:RelatedEntities ",Ga.collection," />"];
return g.join("")}function h(a,b,c){var d=["<entity ",Ga.xrm,">",g(a,b,c),"</entity>"];return d.join("")}function i(a,b){
return a&&a.hasChildNodes()?a.querySelector(a.localName+" > "+b):null}function j(a,b){var c=i(a,b);return c?c.textContent:"";
}function k(b,c,d,e){var f;return f=new a.OrgService.OptionSetValue,e?f.Value=parseInt(j(c,"value > Value > Value"),10):f.Value=parseInt(j(c,"value > Value"),10),
b&&b.hasOwnProperty(d)&&(f.Label=b[d]),f}function l(b,c){var d,f;return d=new a.OrgService.EntityReference,f="value > ",c&&(f="value > Value > "),
e(i(b,f+"Id"))||(d.Id=j(b,f+"Id")),e(i(b,f+"Name"))||(d.Name=j(b,f+"Name")),e(i(b,f+"LogicalName"))||(d.LogicalName=j(b,f+"LogicalName")),
d}function m(b,c,d,e){var f,g;return f=new a.OrgService.Money,g="value > ",e&&(g="value > Value > "),f.Value=parseFloat(j(c,g+"Value")),
b&&b.hasOwnProperty(d)&&(f.DisplayValue=b[d]),f}function n(b,c){var d,e;return d=new a.OrgService.Guid,e="value",c&&(e="value > Value"),
d.Value=j(b,e),d}function o(a,b){var c;return c=b?parseInt(j(a,"value > Value"),10):parseInt(j(a,"value"),10)}function p(b,c,d,e){
var f,g;return f=new a.OrgService.Decimal,g="value",e&&(g="value > Value"),f.Value=parseFloat(j(c,g),10),b&&b.hasOwnProperty(d)&&(f.DisplayValue=b[d]),
f}function q(b,c,d,e){var f,g;return f=new a.OrgService.Double,g="value",e&&(g="value > Value"),f.Value=parseFloat(j(c,g),10),
b&&b.hasOwnProperty(d)&&(f.DisplayValue=b[d]),f}function r(b,c,d,e){var f,g;return f=new a.OrgService.Boolean,g="value",e&&(g="value > Value"),
f.Value="true"===j(c,g),b&&b.hasOwnProperty(d)&&(f.DisplayValue=b[d]),f}function s(b,c,d,e){var f,g,h;return f=new a.OrgService.DateTime,
g="value",e&&(g="value > Value"),f.UTC=j(c,g),b&&b.hasOwnProperty(d)&&(f.DisplayValue=b[d],h=a.Date.parse(f.UTC),f.Value=new Date(h)),
f}function t(a){var b,c;return b=j(a,"key"),c=b.split("."),2!==c.length?null:c[0]}function u(a,b,c){var d,e,f,g;switch(d={
_type:"Entity",EntityLogicalName:j(b,"value > EntityLogicalName")},e=j(b,"value > AttributeLogicalName"),f=i(b,"value > Value").getAttribute("i:type"),
g=j(b,"value > Value"),f){case"c:guid":g=n(b,!0);break;case"a:OptionSetValue":g=k(a,b,c,!0);break;case"a:EntityReference":
g=l(b,!0);break;case"a:Money":g=m(a,b,c,!0);break;case"c:dateTime":g=s(a,b,c,!0);break;case"c:decimal":g=p(a,b,c,!0);break;
case"c:double":g=q(a,b,c,!0);break;case"c:int":g=o(b,!0);break;case"c:boolean":g=r(a,b,c,!0);break;case"a:AliasedValue":alert("Unsupported parsing of multi-tiered aliased/linked entities");
break;case"c:string":}return e&&(d[e]=g),d}function v(a){var b,c;return b={MoreRecords:!1,TotalRecordCount:-1,TotalRecordCountLimitExceeded:!1,
PagingCookie:null},c=a.querySelector("Envelope > Body > RetrieveMultipleResponse > RetrieveMultipleResult"),i(c,"TotalRecordCount")&&!isNaN(parseInt(j(c,"TotalRecordCount"),10))&&(b.TotalRecordCount=parseInt(j(c,"TotalRecordCount"),10)),
i(c,"TotalRecordCountLimitExceeded")&&(b.TotalRecordCountLimitExceeded="true"===j(c,"TotalRecordCountLimitExceeded")),i(c,"PagingCookie")&&(b.PagingCookie=j(c,"PagingCookie").replace(/</g,"&lt;").replace(/>/g,"&gt;").replace(/"/g,"&quot;")),
i(c,"MoreRecords")&&(b.MoreRecords="true"===j(c,"MoreRecords")),b}function w(a,b,d){for(var e,f,g,h,v=0,w=b.childNodes.length;v<w;v++){
switch(e=b.childNodes[v],f=i(e,"value").getAttribute("i:type"),g=j(e,"value"),h=j(e,"key"),f){case"c:guid":g=n(e);break;case"a:OptionSetValue":
g=k(d,e,h);break;case"a:EntityReference":g=l(e);break;case"a:Money":g=m(d,e,h);break;case"c:dateTime":g=s(d,e,h);break;case"c:decimal":
g=p(d,e,h);break;case"c:double":g=q(d,e,h);break;case"c:int":g=o(e);break;case"c:boolean":g=r(d,e,h);break;case"a:AliasedValue":
a.Metadata.RelatedEntityCount++,g=u(d,e,h),h=t(e),h||(a[j(e,"key")]=g[j(e,"value > AttributeLogicalName")]),a.hasOwnProperty(h)?a[h]=c(a[h],g):h&&(a[h]=g),
h=null;break;case"c:string":}h&&(a[h]=g)}}function x(b,c){var d,e,f,g,h,k,l,m;if(d={Metadata:{RelatedEntityCount:0,AttributeCount:0
}},"array"===a.type(c))for(m=c.length,l=0;l<m;l++)d[c[l]]=null;if(e={},f=i(b,"FormattedValues"),null!=f&&f.childNodes.length>0)for(m=f.childNodes.length,
l=0;l<m;l++)k=j(f.childNodes[l],"key"),h=j(f.childNodes[l],"value"),k&&(e[k]=h);return g=i(b,"Attributes"),g.childNodes.length>0&&(d.Metadata.AttributeCount=g.childNodes.length,
d.Metadata.RelatedEntityCount=0,w(d,g,e)),d.Metadata.Id=j(b,"Id"),d.Metadata.LogicalName=j(b,"LogicalName"),d}function y(a){
var b=a.querySelector("Envelope > Body > CreateResponse > CreateResult").textContent;return b}function z(a,b){var c;return c=x(a.querySelector("Envelope > Body > RetrieveResponse > RetrieveResult"),b);
}function A(a){var b,c,d,e,f;if(b=v(a),c=a.querySelector("Envelope > Body > RetrieveMultipleResponse > RetrieveMultipleResult > Entities"),
d=[],c.childNodes.length>0){for(f=c.childNodes.length,e=0;e<f;e++)d.push(x(c.childNodes[e]));return{Entities:d,Paging:b}}
return{Entities:[],Paging:null}}function B(a){var b=a.querySelector("Envelope > Body > ExecuteResponse > ExecuteResult > Results > KeyValuePairOfstringanyType > value"),c=b?b.textContent:null;
return c}function C(a){var b=x(a.querySelector("Envelope > Body > ExecuteResponse > ExecuteResult > Results > KeyValuePairOfstringanyType > value"));
return b}function D(a,b){return{Success:a,Value:b}}function E(a,b,c){var d=['<request i:type="c:'+a+'" ',Ga.xrm," ",Ga.crm,">","<a:Parameters ",Ga.collection,">",c,"</a:Parameters>",'<a:RequestId i:nil="true" />',"<a:RequestName>",b,"</a:RequestName>","</request>"];
return d.join("")}function F(a,b){var c=["<a:KeyValuePairOfstringanyType>","<b:key>",a,"</b:key>",b,"</a:KeyValuePairOfstringanyType>"];
return c.join("")}function G(b,c,d,e){var f;if(4===b.readyState)if(200===b.status)f=a.Xml.loadXml(b.responseText),c&&(f=c(f)),
d(f);else{if(0===b.status)return void(b=null);e(H(b))}b=null}function H(b){var c,d,e,f,g,h,i,j,k,l,m,n,o,p=a.Xml.loadXml(b.responseText);
if(c="Unknown Error (Unable to parse the fault)",d=0,"object"===a.type(p)){try{for(e=p.firstChild.firstChild,j=e.childNodes.length,
i=0;i<j;i++)if(f=e.childNodes[i],"s:Fault"===f.nodeName){for(l=f.childNodes.length,k=0;k<l;k++)for(g=f.childNodes[k],"faultstring"===g.nodeName&&(c=g.textContent),
n=g.length,m=0;m<n;m++)if("ErrorDetails"===g.nodeName){if("2"===d)return;h=this.childNodes[1].textContent,"OperationStatus"===this.childNodes[0].textContent&&(!d||d<h)&&(d=h);
}break}}catch(q){}return o=new Error(c),o.operationStatus=d,o}}function I(a,b,c){var d=['<request i:type="c:AssignRequest" xmlns:a="http://schemas.microsoft.com/xrm/2011/Contracts" xmlns:c="http://schemas.microsoft.com/crm/2011/Contracts">','<a:Parameters xmlns:b="http://schemas.datacontract.org/2004/07/System.Collections.Generic">',"<a:KeyValuePairOfstringanyType>","<b:key>Target</b:key>",b.toXml(),"</a:KeyValuePairOfstringanyType>","<a:KeyValuePairOfstringanyType>","<b:key>Assignee</b:key>",a.toXml(),"</a:KeyValuePairOfstringanyType>","</a:Parameters>",'<a:RequestId i:nil="true" />',"<a:RequestName>Assign</a:RequestName>","</request>"].join("");
return c?ra(d,"Execute"):sa(d,"Execute")}function J(a,b){return I(a,b,!0)}function K(a,b){return I(a,b,!1)}function L(a,b,c,d){
var e,f="",g=b.length,h=0;if(b&&g)for(;h<g;h++)f+=["<a:EntityReference>","<a:Id>",b[h].Id,"</a:Id>","<a:LogicalName>",b[h].LogicalName,"</a:LogicalName>",'<a:Name i:nil="true" />',"</a:EntityReference>"].join("");else f=["<a:EntityReference>","<a:Id>",b.Id,"</a:Id>","<a:LogicalName>",b.LogicalName,"</a:LogicalName>",'<a:Name i:nil="true" />',"</a:EntityReference>"].join("");
return e=['<request i:type="a:AssociateRequest" xmlns:a="http://schemas.microsoft.com/xrm/2011/Contracts">','<a:Parameters xmlns:b="http://schemas.datacontract.org/2004/07/System.Collections.Generic">',"<a:KeyValuePairOfstringanyType>","<b:key>Target</b:key>",'<b:value i:type="a:EntityReference">',"<a:Id>",a.Id,"</a:Id>","<a:LogicalName>",a.LogicalName,"</a:LogicalName>",'<a:Name i:nil="true" />',"</b:value>","</a:KeyValuePairOfstringanyType>","<a:KeyValuePairOfstringanyType>","<b:key>Relationship</b:key>",'<b:value i:type="a:Relationship">','<a:PrimaryEntityRole i:nil="true" />',"<a:SchemaName>",c,"</a:SchemaName>","</b:value>","</a:KeyValuePairOfstringanyType>","<a:KeyValuePairOfstringanyType>","<b:key>RelatedEntities</b:key>",'<b:value i:type="a:EntityReferenceCollection">',f,"</b:value>","</a:KeyValuePairOfstringanyType>","</a:Parameters>",'<a:RequestId i:nil="true" />',"<a:RequestName>Associate</a:RequestName>","</request>"].join(""),
d?ra(e,"Execute"):sa(e,"Execute")}function M(a,b,c){return L(a,b,c,!0)}function N(a,b,c){return L(a,b,c,!1)}function O(a,b,c,d){
var e,f="",g=b.length,h=0;if(b&&g)for(;h<g;h++)f+=["<a:EntityReference>","<a:Id>",b[h].Id,"</a:Id>","<a:LogicalName>",b[h].LogicalName,"</a:LogicalName>",'<a:Name i:nil="true" />',"</a:EntityReference>"].join("");else f=["<a:EntityReference>","<a:Id>",b.Id,"</a:Id>","<a:LogicalName>",b.LogicalName,"</a:LogicalName>",'<a:Name i:nil="true" />',"</a:EntityReference>"].join("");
return e=['<request i:type="a:DisassociateRequest" xmlns:a="http://schemas.microsoft.com/xrm/2011/Contracts">','<a:Parameters xmlns:b="http://schemas.datacontract.org/2004/07/System.Collections.Generic">',"<a:KeyValuePairOfstringanyType>","<b:key>Target</b:key>",'<b:value i:type="a:EntityReference">',"<a:Id>",a.Id,"</a:Id>","<a:LogicalName>",a.LogicalName,"</a:LogicalName>",'<a:Name i:nil="true" />',"</b:value>","</a:KeyValuePairOfstringanyType>","<a:KeyValuePairOfstringanyType>","<b:key>Relationship</b:key>",'<b:value i:type="a:Relationship">','<a:PrimaryEntityRole i:nil="true" />',"<a:SchemaName>",c,"</a:SchemaName>","</b:value>","</a:KeyValuePairOfstringanyType>","<a:KeyValuePairOfstringanyType>","<b:key>RelatedEntities</b:key>",'<b:value i:type="a:EntityReferenceCollection">',f,"</b:value>","</a:KeyValuePairOfstringanyType>","</a:Parameters>",'<a:RequestId i:nil="true" />',"<a:RequestName>Disassociate</a:RequestName>","</request>"].join(""),
d?ra(e,"Execute"):sa(e,"Execute")}function P(a,b,c){return O(a,b,c,!0)}function Q(a,b,c){return O(a,b,c,!1)}function R(a,b,c){
var d=['<request i:type="b:ExecuteWorkflowRequest" xmlns:a="http://schemas.microsoft.com/xrm/2011/Contracts" xmlns:b="http://schemas.microsoft.com/crm/2011/Contracts">','<a:Parameters xmlns:c="http://schemas.datacontract.org/2004/07/System.Collections.Generic">',"<a:KeyValuePairOfstringanyType>","<c:key>EntityId</c:key>",'<c:value i:type="d:guid" xmlns:d="http://schemas.microsoft.com/2003/10/Serialization/">',a,"</c:value>","</a:KeyValuePairOfstringanyType>","<a:KeyValuePairOfstringanyType>","<c:key>WorkflowId</c:key>",'<c:value i:type="d:guid" xmlns:d="http://schemas.microsoft.com/2003/10/Serialization/">',b,"</c:value>","</a:KeyValuePairOfstringanyType>","</a:Parameters>",'<a:RequestId i:nil="true" />',"<a:RequestName>ExecuteWorkflow</a:RequestName>","</request>"].join("");
return c?ra(d,"Execute",B):sa(d,"Execute",B)}function S(a,b){return R(a,b,!0)}function T(a,b){return R(a,b,!1)}function U(a,b,c,d){
c&&!/\S/.test(c)||(c="All");var e=['<request i:type="b:InitializeFromRequest" xmlns:a="http://schemas.microsoft.com/xrm/2011/Contracts" xmlns:b="http://schemas.microsoft.com/crm/2011/Contracts">','<a:Parameters xmlns:c="http://schemas.datacontract.org/2004/07/System.Collections.Generic">',"<a:KeyValuePairOfstringanyType>","<c:key>EntityMoniker</c:key>",'<c:value i:type="a:EntityReference">',"<a:Id>",a.Id,"</a:Id>","<a:LogicalName>",a.LogicalName,"</a:LogicalName>",'<a:Name i:nil="true" />',"</c:value>","</a:KeyValuePairOfstringanyType>","<a:KeyValuePairOfstringanyType>","<c:key>TargetEntityName</c:key>",'<c:value i:type="d:string" xmlns:d="http://www.w3.org/2001/XMLSchema">',b,"</c:value>","</a:KeyValuePairOfstringanyType>","<a:KeyValuePairOfstringanyType>","<c:key>TargetFieldType</c:key>",'<c:value i:type="b:TargetFieldType">',c,"</c:value>","</a:KeyValuePairOfstringanyType>","</a:Parameters>",'<a:RequestId i:nil="true" />',"<a:RequestName>InitializeFrom</a:RequestName>","</request>"].join("");
return d?ra(e,"Execute",C):sa(e,"Execute",C)}function V(a,b,c){return U(a,b,c,!0)}function W(a,b,c){return U(a,b,c,!1)}function X(a,b,c){
var d;return d=["<entityName>",a,"</entityName>","<id>",b,"</id>"].join(""),c?ra(d,"Delete"):sa(d,"Delete")}function Y(a,b){
return X(a,b,!0)}function Z(a,b){return X(a,b,!1)}function $(a,b,c){var d;return d=h(a,null,b),c?ra(d,"Create",y):sa(d,"Create",y);
}function _(a,b){return $(a,b,!0)}function aa(a,b){return $(a,b,!1)}function ba(a,b,c,d){var e;return e=h(a,b,c),d?ra(e,"Update"):sa(e,"Update");
}function ca(a,b,c){return ba(a,b,c,!0)}function da(a,b,c){return ba(a,b,c,!1)}function ea(a,c,d,e){var f;return f=["<entityName>",a,"</entityName>","<id>",c,"</id>"],
f.push(b(d)),e?ra(f.join(""),"Retrieve",function(a){return z(a,d)}):sa(f.join(""),"Retrieve",z)}function fa(a,b,c){return ea(a,b,c,!0);
}function ga(a,b,c){return ea(a,b,c,!1)}function ha(b,c){var d;return b=a.Xml.xmlEncode(b),d=['<query i:type="a:FetchExpression" ',Ga.xrm,">","<a:Query>",b,"</a:Query>","</query>"].join(""),
c?ra(d,"RetrieveMultiple",A):sa(d,"RetrieveMultiple",A)}function ia(a){return ha(a,!0)}function ja(a){return ha(a,!1)}function ka(b,c,d,e){
var f,g;return"number"===a.type(c)&&(c=new a.OrgService.OptionSetValue(c)),"number"===a.type(d)&&(d=new a.OrgService.OptionSetValue(d)),
f=[F("EntityMoniker",b.toXml()),F("State",c.toXml()),F("Status",d.toXml())].join(""),g=E("SetStateRequest","SetState",f),
e?ra(g,"Execute"):sa(g,"Execute")}function la(a,b,c){return ka(a,b,c,!0)}function ma(a,b,c){return ka(a,b,c,!1)}function na(b,c,d){
var e,f,g,h=[];for(var i in c)c.hasOwnProperty(i)&&(g=null==c[i]?new wa:"undefined"!==a.type(c[i].toXml)?c[i].toXml():"number"===a.type(c[i])?['<b:value i:type="c:int" ',Ga.xml,">",a.Xml.xmlEncode(c[i]),"</b:value>"].join(""):['<b:value i:type="c:string" ',Ga.xml,">",a.Xml.xmlEncode(c[i]),"</b:value>"].join(""),
h.push(F(i,g)));return e=["<request ",Ga.xrm," ",Ga.crm,">","<a:Parameters ",Ga.collection,">",h.join(""),"</a:Parameters>",'<a:RequestId i:nil="true" />',"<a:RequestName>",b,"</a:RequestName>","</request>"].join(""),
d?ra(e,"Execute",function(a){return f={},w(f,a.querySelector("Envelope > Body > ExecuteResponse > ExecuteResult > Results")),
f}):sa(e,"Execute",function(a){return f={},w(f,a.querySelector("Envelope > Body > ExecuteResponse > ExecuteResult > Results")),
f})}function oa(a,b){return na(a,b,!0)}function pa(a,b){return na(a,b,!1)}function qa(b,c,e,f){var g,h,i,j;return d(f)&&(f=!0),
g=['<s:Envelope xmlns:s="http://schemas.xmlsoap.org/soap/envelope/">',"<s:Body>","<",c,' xmlns="http://schemas.microsoft.com/xrm/2011/Contracts/Services"',' xmlns:i="http://www.w3.org/2001/XMLSchema-instance">',b,"</",c,">","</s:Body>","</s:Envelope>"].join(""),
h=new XMLHttpRequest,h.open("POST",a.getClientUrl()+"/XRMServices/2011/Organization.svc/web",f),h.setRequestHeader("Accept","application/xml, text/xml, */*"),
h.setRequestHeader("Content-Type","text/xml; charset=utf-8"),h.setRequestHeader("SOAPAction","http://schemas.microsoft.com/xrm/2011/Contracts/Services/IOrganizationService/"+c),
f?(j=a.Promise.defer(),h.onreadystatechange=function(){G(h,e,j.resolve,j.reject)},h.send(g),j.promise):(h.send(g),G(h,e,function(a){
i=D(!0,a)},function(a){i=D(!1,a)}),i)}function ra(a,b,c){return qa(a,b,c,!0)}function sa(a,b,c){return qa(a,b,c,!1)}function ta(a,b,c,d,e){
var f=JSON.stringify(b),g=d||"sonoma",h=e||"webservice",i=g+"_data",j=g+"_error",k=["<fetch>",'<entity name="',g,"_",h,'">','<attribute name="',g,'_data" />','<attribute name="',g,'_error" />',"<filter>",'<condition attribute="',g,'_logicclassname" operator="eq" value="',encodeURIComponent(a),'" />','<condition attribute="',g,'_data" operator="eq" value="',encodeURIComponent(f),'" />','<condition attribute="',g,'_istransactional" operator="eq" value="',!!c,'" />',"</filter>","</entity>","</fetch>"].join("");
return ha(k,!0).then(function(a){if(a&&a.Entities&&a.Entities.length){var b="";if(!a.Entities[0][i])throw null!==a.Entities[0][j]?new Error(a.Entities[0][j]):new Error('{"Error": "Unexpected response received."}');
return b+=a.Entities[0][i],b=JSON.parse(b)}throw new Error('{"Error": "Unexpected response received."}')})}function ua(a,b,c,d,e){
var f,g="",h=JSON.stringify(b),i=d||"sonoma",j=e||"webservice",k=i+"_data",l=i+"_error",m=["<fetch>",'<entity name="',i,"_",j,'">','<attribute name="',i,'_data" />','<attribute name="',i,'_error" />',"<filter>",'<condition attribute="',i,'_logicclassname" operator="eq" value="',encodeURIComponent(a),'" />','<condition attribute="',i,'_data" operator="eq" value="',encodeURIComponent(h),'" />','<condition attribute="',i,'_istransactional" operator="eq" value="',!!c,'" />',"</filter>","</entity>","</fetch>"].join("");
return f=ha(m,!1),f&&f.Value&&f.Value.Entities&&f.Value.Entities.length?f.Value.Entities[0][k]?(g+=f.Value.Entities[0][k],
g=JSON.parse(g),D(!0,g)):null!==f.Value.Entities[0][l]?D(!1,new Error(f.Value.Entities[0][l])):D(!1,new Error('{"Error": "Unexpected response received."}')):D(!1,new Error('{"Error": "Unexpected response received."}'));
}function va(){}function wa(){if(!(this instanceof wa))return new wa}function xa(b,c){return this instanceof xa?(this._type=a.OrgService.attributeTypes.Boolean,
this.Value=b,void(this.DisplayValue=c)):new xa(b,c)}function ya(b,c,d){return this instanceof ya?(this._type=a.OrgService.attributeTypes.DateTime,
this.Value=b,this.DisplayValue=c,void(this.UTC=d)):new ya(b,c,d)}function za(b,c){return this instanceof za?(this._type=a.OrgService.attributeTypes.Decimal,
this.Value=b,void(this.DisplayValue=c)):new za(b,c)}function Aa(b,c){return this instanceof Aa?(this._type=a.OrgService.attributeTypes.Double,
this.Value=b,void(this.DisplayValue=c)):new Aa(b,c)}function Ba(b,c,d){return this instanceof Ba?(this._type=a.OrgService.attributeTypes.EntityReference,
this.Id=b,this.LogicalName=c,void(this.Name=d)):new Ba(b,c,d)}function Ca(b,c){return this instanceof Ca?(this._type=a.OrgService.attributeTypes.Guid,
void(this.Value=b)):new Ca(b,c)}function Da(b,c){return this instanceof Da?(this._type=a.OrgService.attributeTypes.Money,
this.Value=b,void(this.DisplayValue=c)):new Da(b,c)}function Ea(b,c){return this instanceof Ea?(this._type=a.OrgService.attributeTypes.OptionSetValue,
this.Value=b,void(this.Label=c)):new Ea(b,c)}function Fa(a){return this instanceof Fa?(this._type="ActivityPartyArray",void(this.EntityReferences=a)):new Fa(a);
}var Ga={xrm:'xmlns:a="http://schemas.microsoft.com/xrm/2011/Contracts"',crm:'xmlns:c="http://schemas.microsoft.com/crm/2011/Contracts"',
collection:'xmlns:b="http://schemas.datacontract.org/2004/07/System.Collections.Generic"',arrays:'xmlns:b="http://schemas.microsoft.com/2003/10/Serialization/Arrays"',
xml:'xmlns:c="http://www.w3.org/2001/XMLSchema"',serialization:'xmlns:c="http://schemas.microsoft.com/2003/10/Serialization/"'
},Ha={Money:"Money",OptionSetValue:"OptionSetValue",Boolean:"Boolean",EntityReference:"EntityReference",DateTime:"DateTime",
Decimal:"Decimal",Double:"Double",Guid:"Guid"};return va.prototype.toXml=function(){alert("toXml() is not implemented for the object "+this._internalGetName()+".");
},va.prototype.toString=function(){return"Not implemented"},va.subClass=function(a){var b;b=/\W*function\s+([\w\$]+)\(/,a.prototype=new va,
a.prototype._internalGetName=function(){var c;return c=b.exec(a.toString())||[],c[1]||"No Name"}},va.subClass(wa),wa.prototype.toXml=function(){
return'<b:value i:nil="true" />'},wa.prototype.toString=function(){return"null"},va.subClass(xa),xa.prototype.toXml=function(){
var a;return a=['<b:value i:type="c:boolean" ',Ga.xml,">",this.Value,"</b:value>"].join("")},xa.prototype.toString=function(){
return this.DisplayValue?this.DisplayValue:this.Value},va.subClass(ya),ya.prototype.toXml=function(){var b;return b=['<b:value i:type="c:dateTime" ',Ga.xml,">",a.Date.toISOString(this.Value),"</b:value>"].join("");
},ya.prototype.toString=function(){return this.DisplayValue?this.DisplayValue:this.Value},va.subClass(za),za.prototype.toXml=function(){
var a;return a=['<b:value i:type="c:decimal" ',Ga.xml,">",this.Value,"</b:value>"].join("")},za.prototype.toString=function(){
return this.DisplayValue?this.DisplayValue:this.Value},va.subClass(Aa),Aa.prototype.toXml=function(){var a;return a=['<b:value i:type="c:double" ',Ga.xml,">",this.Value,"</b:value>"].join("");
},Aa.prototype.toString=function(){return null!=this.DisplayValue?this.DisplayValue:this.Value},va.subClass(Ba),Ba.prototype.toXml=function(){
var a;return a=['<b:value i:type="a:EntityReference">',"<a:Id>"+this.Id+"</a:Id>","<a:LogicalName>"+this.LogicalName+"</a:LogicalName>",'<a:Name i:nil="true" />',"</b:value>"].join("");
},Ba.prototype.toString=function(){return this.Name},va.subClass(Ca),Ca.prototype.toXml=function(){var a=['<b:value i:type="c:guid" ',Ga.serialization,">",this.Value,"</b:value>"].join("");
return a},Ca.prototype.toString=function(){return this.Value},va.subClass(Da),Da.prototype.toXml=function(){var a=['<b:value i:type="a:Money">',"<a:Value>"+this.Value+"</a:Value>","</b:value>"].join("");
return a},Da.prototype.toString=function(){return this.DisplayValue?this.DisplayValue:this.Value},va.subClass(Ea),Ea.prototype.toXml=function(){
var a=['<b:value i:type="a:OptionSetValue">',"<a:Value>"+this.Value+"</a:Value>","</b:value>"].join("");return a},Ea.prototype.toString=function(){
return this.Label?this.Label:this.Value},va.subClass(Fa),Fa.prototype.toXml=function(){var a,b,c,d=[],e=0;for(a=this.EntityReferences.length,
e=0;e<a;e++)b=["<a:Entity>","<a:Attributes>","<a:KeyValuePairOfstringanyType>","<b:key>partyid</b:key>",'<b:value i:type="a:EntityReference">',"<a:Id>",this.EntityReferences[e].Id,"</a:Id>","<a:LogicalName>",this.EntityReferences[e].LogicalName,"</a:LogicalName>",'<a:Name i:nil="true" />',"</b:value>","</a:KeyValuePairOfstringanyType>","</a:Attributes>",'<a:EntityState i:nil="true" />',"<a:FormattedValues />","<a:Id>00000000-0000-0000-0000-000000000000</a:Id>","<a:LogicalName>activityparty</a:LogicalName>","<a:RelatedEntities />","</a:Entity>"].join(""),
d.push(b);return c=['<b:value i:type="a:ArrayOfEntity">',d.join(""),"</b:value>"].join("")},Fa.prototype.toString=function(){
return""},{attributeTypes:Ha,create:_,createSync:aa,update:ca,updateSync:da,retrieve:fa,retrieveSync:ga,retrieveMultiple:ia,
retrieveMultipleSync:ja,setState:la,setStateSync:ma,execute:ra,executeSync:sa,executeAction:oa,executeActionSync:pa,executeWebService:ta,
executeWebServiceSync:ua,deleteRecord:Y,deleteRecordSync:Z,NullValue:wa,Boolean:xa,DateTime:ya,Decimal:za,Double:Aa,EntityReference:Ba,
Guid:Ca,Money:Da,OptionSetValue:Ea,ActivityPartyArray:Fa,executeWorkflow:S,executeWorkflowSync:T,initializeFromRequest:V,
initializeFromRequestSync:W,assign:J,assignSync:K,associate:M,associateSync:N,disassociate:P,disassociateSync:Q}}(),a.Metadata=function(){
function b(a){var b=["<soapenv:Envelope ",sa.soapenv,">","<soapenv:Body>",a,"</soapenv:Body>","</soapenv:Envelope>"];return b.join("");
}function c(b,c,d,e){"undefined"===a.type(d)&&(d=!0);var f,g=a.getClientUrl()+ra,h=new XMLHttpRequest;return h.open("POST",g,d),
h.setRequestHeader("Accept","application/xml, text/xml, */*"),h.setRequestHeader("Content-Type","text/xml; charset=utf-8"),
h.setRequestHeader("SOAPAction","http://schemas.microsoft.com/xrm/2011/Contracts/Services/IOrganizationService/Execute"),
d&&(f=a.Promise.defer(),h.onreadystatechange=function(){c(h,e,f.resolve,f.reject)}),h.send(b),d?f.promise:c(h,e)}function d(a,b){
return{Success:b,Value:a}}function e(b){if("12029"===b.status)return new Error("The attempt to connect to the server failed.");
if("12007"===b.status)return new Error("The server name could not be resolved.");var c,d,e,f,g,h,i,j=a.Xml.loadXml(b.responseText),k="Unknown error (unable to parse the fault)";
if("object"===a.type(j)&&j.firstChild&&j.firstChild.firstChild)for(c=j.firstChild.firstChild,g=c.childNodes.length,f=0;f<g;f++)if(d=c.childNodes[f],
"s:Fault"===d.nodeName){for(i=d.childNodes.length,h=0;h<i;h++)if(e=d.childNodes[h],"faultstring"===e.nodeName){k=e.text||e.textContent;
break}break}return new Error(k)}function f(a,b){return a&&a.hasChildNodes()?a.querySelector(a.localName+" > "+b):null}function g(a,b){
var c=f(a,b);return c?c.textContent:""}function h(b){if("string"===a.type(b))return b;var c="Entity";return 2&b&&(c+=" Attributes"),
4&b&&(c+=" Privileges"),8&b&&(c+=" Relationships"),c}function i(b){if("number"===a.type(b))return b;var c=1;return b.indexOf("Attributes")>=0&&(c+=2),
b.indexOf("Privileges")>=0&&(c+=4),b.indexOf("Relationships")>=0&&(c+=8),c}function j(b,c){var d,e,f,g=c,h=0,i=a.Cache.get("Metadata.Entities",b.LogicalName);
return i&&(h=i.FilterLevel,d=i.Metadata,g=c|h,e=g^c,2&e&&(b.Attributes=d.Attributes),4&e&&(b.Privileges=d.Privileges),8&e&&(b.OneToManyRelationships=d.OneToManyRelationships,
b.ManyToOneRelationships=d.ManyToOneRelationships,b.ManyToManyRelationships=d.ManyToManyRelationships)),f={FilterLevel:g,
Metadata:b},a.Cache.set("Metadata.Entities",b.LogicalName,f),h}function k(a){return 0===a.childNodes.length?null:{MetadataId:g(a,"MetadataId"),
Description:q(f(a,"Description")),DisplayName:q(f(a,"DisplayName")),IsCustomOptionSet:r(f(a,"IsCustomOptionSet")),IsCustomizable:o(f(a,"IsCustomizable")),
IsGlobal:r(f(a,"IsGlobal")),IsManaged:r(f(a,"IsManaged")),Name:g(a,"Name"),OptionSetType:g(a,"OptionSetType"),FalseOption:{
MetadataId:g(a,"FalseOption > MetadataId"),Description:q(f(a,"FalseOption > Description")),IsManaged:r(f(a,"FalseOption > IsManaged")),
Label:q(f(a,"FalseOption > Label")),Value:s(f(a,"FalseOption > Value"))},TrueOption:{MetadataId:g(a,"TrueOption > MetadataId"),
Description:q(f(a,"TrueOption > Description")),IsManaged:r(f(a,"TrueOption > IsManaged")),Label:q(f(a,"TrueOption > Label")),
Value:s(f(a,"TrueOption > Value"))}}}function l(a){var b,c,d,e,h=[];for(e=a.childNodes.length,d=0;d<e;d++)b=a.childNodes[d],
c={MetadataId:g(b,"MetadataId"),Description:q(f(b,"Description")),IsManaged:r(f(b,"IsManaged")),Label:q(f(b,"Label")),Value:s(f(b,"Value")),
State:f(b,"State")?s(f(b,"State")):null},h.push(c);return h}function m(a){return 0===a.childNodes.length?null:{MetadataId:g(a,"MetadataId"),
Description:q(f(a,"Description")),DisplayName:q(f(a,"DisplayName")),IsCustomOptionSet:r(f(a,"IsCustomOptionSet")),IsCustomizable:o(f(a,"IsCustomizable")),
IsGlobal:r(f(a,"IsGlobal")),IsManaged:r(f(a,"IsManaged")),Name:g(a,"Name"),OptionSetType:g(a,"OptionSetType"),Options:l(f(a,"Options"))
}}function n(a){var b=s(f(a,"Order"));return isNaN(b)&&(b=null),{Behavior:g(a,"Behavior"),Group:g(a,"Group"),Label:q(f(a,"Label")),
Order:b}}function o(a){return a&&a.textContent?{CanBeChanged:"true"===g(a,"CanBeChanged"),ManagedPropertyLogicalName:g(a,"ManagedPropertyLogicalName"),
Value:"true"===g(a,"Value")}:null}function p(a){return a&&a.textContent?{IsManaged:"true"===g(a,"IsManaged"),Label:g(a,"Label"),
LanguageCode:s(f(a,"LanguageCode"))}:null}function q(a){if(!a||!a.textContent)return null;var b,c,d=f(a,"LocalizedLabels"),e=f(a,"UserLocalizedLabel"),g=[],h=0;
if(d)for(c=d.childNodes.length,h=0;h<c;h++)b=d.childNodes[h],g.push(p(b));return{LocalizedLabels:g,UserLocalizedLabel:e?p(e):null
}}function r(a){return a&&a.textContent?"true"===a.textContent:null}function s(a){return a&&a.textContent?parseInt(a.textContent,10):null;
}function t(a){return a&&a.textContent?{CanBeChanged:"true"===g(a,"CanBeChanged"),ManagedPropertyLogicalName:g(a,"ManagedPropertyLogicalName"),
Value:g(a,"Value")}:null}function u(a){return{CanBeBasic:"true"===g(a,"CanBeBasic"),CanBeDeep:"true"===g(a,"CanBeDeep"),CanBeGlobal:"true"===g(a,"CanBeGlobal"),
CanBeLocal:"true"===g(a,"CanBeLocal"),Name:g(a,"Name"),PrivilegeId:g(a,"PrivilegeId"),PrivilegeType:g(a,"PrivilegeType")};
}function v(a){return{MetadataId:g(a,"MetadataId"),IsCustomRelationship:"true"===g(a,"IsCustomRelationship"),IsCustomizable:o(f(a,"IsCustomizable")),
IsManaged:"true"===g(a,"IsManaged"),IsValidForAdvancedFind:"true"===g(a,"IsValidForAdvancedFind"),SchemaName:g(a,"SchemaName"),
SecurityTypes:g(a,"SecurityTypes"),AssociatedMenuConfiguration:n(f(a,"AssociatedMenuConfiguration")),CascadeConfiguration:{
Assign:g(a,"CascadeConfiguration > Assign"),Delete:g(a,"CascadeConfiguration > Delete"),Merge:g(a,"CascadeConfiguration > Merge"),
Reparent:g(a,"CascadeConfiguration > Reparent"),Share:g(a,"CascadeConfiguration > Share"),Unshare:g(a,"CascadeConfiguration > Unshare")
},ReferencedAttribute:g(a,"ReferencedAttribute"),ReferencedEntity:g(a,"ReferencedEntity"),ReferencingAttribute:g(a,"ReferencingAttribute"),
ReferencingEntity:g(a,"ReferencingEntity")}}function w(a){return{MetadataId:g(a,"MetadataId"),IsCustomRelationship:"true"===g(a,"IsCustomRelationship"),
IsCustomizable:o(f(a,"IsCustomizable")),IsManaged:"true"===g(a,"IsManaged"),IsValidForAdvancedFind:"true"===g(a,"IsValidForAdvancedFind"),
SchemaName:g(a,"SchemaName"),SecurityTypes:g(a,"SecurityTypes"),Entity1AssociatedMenuConfiguration:n(f(a,"Entity1AssociatedMenuConfiguration")),
Entity1IntersectAttribute:g(a,"Entity1IntersectAttribute"),Entity1LogicalName:g(a,"Entity1LogicalName"),Entity2AssociatedMenuConfiguration:n(f(a,"Entity2AssociatedMenuConfiguration")),
Entity2IntersectAttribute:g(a,"Entity2IntersectAttribute"),Entity2LogicalName:g(a,"Entity2LogicalName"),IntersectEntityName:g(a,"IntersectEntityName")
}}function x(a){var b,c,d,e,h,i,j,k,l,m,n,p,t,x,y,z,A,B,C,D,E=f(a,"Attributes"),F=0;if(E)for(D=E.childNodes.length,F=0;F<D;F++)b||(b={}),
c=E.childNodes[F],d=new V(c),b[d.LogicalName]=d;if(e=f(a,"Privileges"))for(D=e.childNodes.length,F=0;F<D;F++)i||(i={}),h=e.childNodes[F],
j=u(h),i[j.Name]=j;if(k=f(a,"OneToManyRelationships"))for(D=k.childNodes.length,F=0;F<D;F++)m||(m={}),l=k.childNodes[F],n=v(l),
m[n.SchemaName]=n;if(p=f(a,"ManyToOneRelationships"))for(D=p.childNodes.length,F=0;F<D;F++)x||(x={}),t=p.childNodes[F],y=v(t),
x[y.SchemaName]=y;if(z=f(a,"ManyToManyRelationships"))for(D=z.childNodes.length,F=0;F<D;F++)B||(B={}),A=z.childNodes[F],C=w(A),
B[C.SchemaName]=C;return{ActivityTypeMask:s(f(a,"ActivityTypeMask")),Attributes:b,AutoRouteToOwnerQueue:r(f(a,"AutoRouteToOwnerQueue")),
CanBeInManyToMany:o(f(a,"CanBeInManyToMany")),CanBePrimaryEntityInRelationship:o(f(a,"CanBePrimaryEntityInRelationship")),
CanBeRelatedEntityInRelationship:o(f(a,"CanBeRelatedEntityInRelationship")),CanCreateAttributes:o(f(a,"CanCreateAttributes")),
CanCreateCharts:o(f(a,"CanCreateCharts")),CanCreateForms:o(f(a,"CanCreateForms")),CanCreateViews:o(f(a,"CanCreateViews")),
CanModifyAdditionalSettings:o(f(a,"CanModifyAdditionalSettings")),CanTriggerWorkflow:r(f(a,"CanTriggerWorkflow")),Description:q(f(a,"Description")),
DisplayCollectionName:q(f(a,"DisplayCollectionName")),DisplayName:q(f(a,"DisplayName")),IconLargeName:g(a,"IconLargeName"),
IconMediumName:g(a,"IconMediumName"),IconSmallName:g(a,"IconSmallName"),IsActivity:r(f(a,"IsActivity")),IsActivityParty:r(f(a,"IsActivityParty")),
IsAuditEnabled:o(f(a,"IsAuditEnabled")),IsAvailableOffline:r(f(a,"IsAvailableOffline")),IsChildEntity:r(f(a,"IsChildEntity")),
IsConnectionsEnabled:o(f(a,"IsConnectionsEnabled")),IsCustomEntity:r(f(a,"IsCustomEntity")),IsCustomizable:o(f(a,"IsCustomizable")),
IsDocumentManagementEnabled:r(f(a,"IsDocumentManagementEnabled")),IsDuplicateDetectionEnabled:o(f(a,"IsDuplicateDetectionEnabled")),
IsEnabledForCharts:r(f(a,"IsEnabledForCharts")),IsImportable:r(f(a,"IsImportable")),IsIntersect:r(f(a,"IsIntersect")),IsMailMergeEnabled:o(f(a,"IsMailMergeEnabled")),
IsManaged:r(f(a,"IsManaged")),IsMappable:o(f(a,"IsMappable")),IsReadingPaneEnabled:r(f(a,"IsReadingPaneEnabled")),IsRenameable:o(f(a,"IsRenameable")),
IsValidForAdvancedFind:r(f(a,"IsValidForAdvancedFind")),IsValidForQueue:o(f(a,"IsValidForQueue")),IsVisibleInMobile:o(f(a,"IsVisibleInMobile")),
LogicalName:f(a,"LogicalName").textContent,ManyToManyRelationships:B,ManyToOneRelationships:x,MetadataId:f(a,"MetadataId").textContent,
ObjectTypeCode:s(f(a,"ObjectTypeCode")),OneToManyRelationships:m,OwnershipType:g(a,"OwnershipType"),PrimaryIdAttribute:g(a,"PrimaryIdAttribute"),
PrimaryNameAttribute:g(a,"PrimaryNameAttribute"),Privileges:i,RecurrenceBaseEntityLogicalName:g(a,"RecurrenceBaseEntityLogicalName"),
ReportViewName:g(a,"ReportViewName"),SchemaName:g(a,"SchemaName"),toString:function(){return this.LogicalName}}}function y(a,c){
var d=['<Execute xmlns="http://schemas.microsoft.com/xrm/2011/Contracts/Services" ','xmlns:i="http://www.w3.org/2001/XMLSchema-instance">','<request i:type="a:RetrieveOptionSetRequest" ','xmlns:a="http://schemas.microsoft.com/xrm/2011/Contracts">','<a:Parameters xmlns:b="http://schemas.datacontract.org/2004/07/System.Collections.Generic">',"<a:KeyValuePairOfstringanyType>","<b:key>MetadataId</b:key>",'<b:value i:type="ser:guid" xmlns:ser="http://schemas.microsoft.com/2003/10/Serialization/">',"00000000-0000-0000-0000-000000000000","</b:value>","</a:KeyValuePairOfstringanyType>","<a:KeyValuePairOfstringanyType>","<b:key>Name</b:key>",'<b:value i:type="c:string" xmlns:c="http://www.w3.org/2001/XMLSchema">',a,"</b:value>","</a:KeyValuePairOfstringanyType>","<a:KeyValuePairOfstringanyType>","<b:key>RetrieveAsIfPublished</b:key>",'<b:value i:type="c:boolean" xmlns:c="http://www.w3.org/2001/XMLSchema">',!!c,"</b:value>","</a:KeyValuePairOfstringanyType>","</a:Parameters>",'<a:RequestId i:nil="true" />',"<a:RequestName>RetrieveOptionSet</a:RequestName>","</request>","</Execute>"];
return b(d.join(""))}function z(b,c,d,f){var g,h;4===b.readyState&&(200===b.status?(g=a.Xml.loadXml(b.responseText),h=new m(g.querySelector("ExecuteResult value")),
a.Cache.set("Metadata.GlobalOptionSets",c,h),d(h)):f(e(b))),b=null}function A(b,c){var f,g,h;return 200===b.status?(f=a.Xml.loadXml(b.responseText),
g=new m(f.querySelector("ExecuteResult value")),a.Cache.set("Metadata.GlobalOptionSets",c,g),b=null,d(g,!0)):(h=e(b),b=null,
d(h,!1))}function B(b,e,f){var g,h,i=a.Cache.get("Metadata.GlobalOptionSets",b);return i?f?new a.Promise.Promise(function(a){
a(i)}):d(i,!0):(g=y(b,e),h=f?z:A,c(g,h,f,b))}function C(a,b){return B(a,b,!0)}function D(a,b){return B(a,b,!1)}function E(a){
var b=U(a);return b.MaxValue=g(a,"MaxValue"),b.MinValue=g(a,"MinValue"),b}function F(a){var b=U(a);return b.DefaultValue=r(f(a,"DefaultValue")),
b.OptionSet=k(f(a,"OptionSet")),b}function G(a){var b=U(a);return b.Format=g(a,"Format"),b.ImeMode=g(a,"ImeMode"),b}function H(a){
var b=U(a);return b.ImeMode=g(a,"ImeMode"),b.MaxValue=g(a,"MaxValue"),b.MinValue=g(a,"MinValue"),b.Precision=s(f(a,"Precision")),
b}function I(a){var b=U(a);return b.ImeMode=g(a,"ImeMode"),b.MaxValue=g(a,"MaxValue"),b.MinValue=g(a,"MinValue"),b.Precision=s(f(a,"Precision")),
b}function J(a){return P(a)}function K(a){var b=U(a);return b.Format=g(a,"Format"),b.MaxValue=s(f(a,"MaxValue")),b.MinValue=s(f(a,"MinValue")),
b}function L(a){var b,c,d,e,g=U(a);if(g.Targets=[],b=f(a,"Targets"),b&&b.childNodes)for(c=0,d=b.childNodes.length;c<d;c++)e=b.childNodes[c],
(e.text||e.textContent)&&g.Targets.push(e.text||e.textContent);return g}function M(a){var b=U(a);return b.ManagedPropertyLogicalName=g(a,"ManagedPropertyLogicalName"),
b.ParentAttributeName=g(a,"ParentAttributeName"),b.ParentComponentType=s(f(a,"ParentComponentType")),b.ValueAttributeTypeCode=g(a,"ValueAttributeTypeCode"),
b}function N(a){var b=U(a);return b.Format=g(a,"Format"),b.ImeMode=g(a,"ImeMode"),b.MaxLength=s(f(a,"MaxLength")),b}function O(a){
var b=U(a);return b.CalculationOf=g(a,"CalculationOf"),b.ImeMode=g(a,"ImeMode"),b.MaxValue=g(a,"MaxValue"),b.MinValue=g(a,"MinValue"),
b.Precision=s(f(a,"Precision")),b.PrecisionSource=s(f(a,"PrecisionSource")),b}function P(a){var b=U(a);return b.DefaultFormValue=r(f(a,"DefaultFormValue")),
b.OptionSet=new m(f(a,"OptionSet")),b}function Q(a){return P(a)}function R(a){return P(a)}function S(a){return P(a)}function T(a){
var b=U(a);return b.Format=g(a,"Format"),b.ImeMode=g(a,"ImeMode"),b.MaxLength=s(f(a,"MaxLength")),b.YomiOf=g(a,"YomiOf"),
b}function U(a){return{AttributeOf:g(a,"AttributeOf"),AttributeType:g(a,"AttributeType"),CanBeSecuredForCreate:r(f(a,"CanBeSecuredForCreate")),
CanBeSecuredForRead:r(f(a,"CanBeSecuredForRead")),CanBeSecuredForUpdate:r(f(a,"CanBeSecuredForUpdate")),CanModifyAdditionalSettings:o(f(a,"CanModifyAdditionalSettings")),
ColumnNumber:s(f(a,"ColumnNumber")),DeprecatedVersion:g(a,"DeprecatedVersion"),Description:q(f(a,"Description")),DisplayName:q(f(a,"DisplayName")),
EntityLogicalName:g(a,"EntityLogicalName"),ExtensionData:null,IsAuditEnabled:o(f(a,"IsAuditEnabled")),IsCustomAttribute:r(f(a,"IsCustomAttribute")),
IsCustomizable:o(f(a,"IsCustomizable")),IsManaged:r(f(a,"IsManaged")),IsPrimaryId:r(f(a,"IsPrimaryId")),IsPrimaryName:r(f(a,"IsPrimaryName")),
IsRenameable:o(f(a,"IsRenameable")),IsSecured:r(f(a,"IsSecured")),IsValidForAdvancedFind:o(f(a,"IsValidForAdvancedFind")),
IsValidForCreate:r(f(a,"IsValidForCreate")),IsValidForRead:r(f(a,"IsValidForRead")),IsValidForUpdate:r(f(a,"IsValidForUpdate")),
LinkedAttributeId:g(a,"LinkedAttributeId"),LogicalName:g(a,"LogicalName"),MetadataId:g(a,"MetadataId"),RequiredLevel:t(f(a,"RequiredLevel")),
SchemaName:g(a,"SchemaName"),toString:function(){return this.LogicalName}}}function V(a){var b=f(a,"AttributeType").textContent;
switch(b){case"BigInt":return E(a);case"Boolean":return F(a);case"CalendarRules":return L(a);case"Customer":return L(a);case"DateTime":
return G(a);case"Decimal":return H(a);case"Double":return I(a);case"EntityName":return J(a);case"Integer":return K(a);case"Lookup":
return L(a);case"ManagedProperty":return M(a);case"Memo":return N(a);case"Money":return O(a);case"Owner":return L(a);case"PartyList":
return L(a);case"Picklist":return Q(a);case"State":return R(a);case"Status":return S(a);case"String":return T(a);case"Uniqueidentifier":
return U(a);case"Virtual":return U(a);default:return U(a)}}function W(a){return ga("Entity",!1).then(function(b){var c;for(c in b)if(b[c].ObjectTypeCode===a)return b[c].LogicalName;
throw new Error('The entity with type code "'+a+'" does not exist.')})}function X(a){return ja("Entity",a,!1).then(function(a){
return a.ObjectTypeCode})}function Y(b){var c=ka(a.Metadata.entityFilters.Entity,b,!1);return c&&c.Success===!0?d(c.Value.ObjectTypeCode,!0):c;
}function Z(a){return ja("Entity",a,!1).then(function(a){return a.PrimaryNameAttribute})}function $(b){var c=ka(a.Metadata.entityFilters.Entity,b,!1);
return c&&c.Success===!0?d(c.Value.PrimaryNameAttribute,!0):c}function _(a,b,c){return pa(a,b,!1).then(function(a){var b,d=0;
for(b=a.OptionSet.Options.length,d=0;d<b;d++)if(a.OptionSet.Options[d].Label.UserLocalizedLabel.Label===c)return a.OptionSet.Options[d].Value;
throw new Error('The option with name "'+c+'" does not exist.')})}function aa(a,b,c){var e,f,g,h=qa(a,b,!1),i=!1,j=0;if(!h||h.Success!==!0)return h;
for(g=h.Value,e=g.OptionSet.Options.length,j=0;j<e;j++)if(g.OptionSet.Options[j].Label.UserLocalizedLabel.Label===c)return d(g.OptionSet.Options[j].Value,!0);
return i?void 0:(f=new Error('The option with name "'+c+'" does not exist.'),d(f,!1))}function ba(a,b,c){return pa(a,b,!1).then(function(a){
var b,d=0;for(b=a.OptionSet.Options.length,d=0;d<b;d++)if(a.OptionSet.Options[d].Value===c)return a.OptionSet.Options[d].Label.UserLocalizedLabel.Label;
throw new Error('The option with value "'+c+'" does not exist.')})}function ca(a,b,c){var e,f,g,h=qa(a,b,!1),i=!1,j=0;if(!h||h.Success!==!0)return h;
for(e=h.Value,f=e.OptionSet.Options.length,j=0;j<f;j++)if(e.OptionSet.Options[j].Value===c)return d(e.OptionSet.Options[j].Label.UserLocalizedLabel.Label,!0);
return i?void 0:(g=new Error('The option with value "'+c+'" does not exist.'),d(g,!1))}function da(b,c,d,f){var g,h;4===b.readyState&&(200===b.status?(h=a.Xml.loadXml(b.responseText),
g=x(h.querySelector("ExecuteResult value")),j(g,c),d(g)):f(e(b))),b=null}function ea(b,c){var f,g,h;return 200===b.status?(g=a.Xml.loadXml(b.responseText),
f=x(g.querySelector("ExecuteResult value")),j(f,c),b=null,d(f,!0)):(h=e(b),b=null,d(h,!1))}function fa(b,c,d,f){if(4===b.readyState)if(200===b.status){
var g,h,i,k,l,m,n={},o=0,p=[];for(h=a.Xml.loadXml(b.responseText),g=h.querySelectorAll("EntityMetadata"),m=g.length,l=0;l<m;l++)i=x(g[l]),
n[i.LogicalName]=i,o=0===l?j(i,c):j(i,c)&o,p.push(i.LogicalName);k=c|o,a.Cache.set("Metadata.AllEntities","FilterLevel",k),
a.Cache.set("Metadata.AllEntities","EntityList",p),d(n)}else f(e(b));b=null}function ga(d,e){var f,g,j,k,l,m,n,o,p;if(f=i(d),
g=a.Cache.get("Metadata.AllEntities","FilterLevel"),j=a.Cache.get("Metadata.AllEntities","EntityList"),g&&j&&(g&f)===f){for(k={},
m=j.length,l=0;l<m;l++)k[j[l]]=a.Cache.get("Metadata.Entities",j[l]).Metadata;return new a.Promise.Promise(function(a){a(k);
})}return n=f,g&&(n=f|g),o=f,g&&(o=n^g),p=['<Execute xmlns="http://schemas.microsoft.com/xrm/2011/Contracts/Services" ','xmlns:i="http://www.w3.org/2001/XMLSchema-instance">','<request i:type="a:RetrieveAllEntitiesRequest" ','xmlns:a="http://schemas.microsoft.com/xrm/2011/Contracts">','<a:Parameters xmlns:b="http://schemas.datacontract.org/2004/07/System.Collections.Generic">',"<a:KeyValuePairOfstringanyType>","<b:key>EntityFilters</b:key>",'<b:value i:type="c:EntityFilters" xmlns:c="http://schemas.microsoft.com/xrm/2011/Metadata">',h(o),"</b:value>","</a:KeyValuePairOfstringanyType>","<a:KeyValuePairOfstringanyType>","<b:key>RetrieveAsIfPublished</b:key>",'<b:value i:type="c:boolean" xmlns:c="http://www.w3.org/2001/XMLSchema">',!!e,"</b:value>","</a:KeyValuePairOfstringanyType>","</a:Parameters>",'<a:RequestId i:nil="true" />',"<a:RequestName>RetrieveAllEntities</a:RequestName>","</request>","</Execute>"],
p=b(p.join("")),c(p,fa,!0,f)}function ha(a,c,d){var e=['<Execute xmlns="http://schemas.microsoft.com/xrm/2011/Contracts/Services" ','xmlns:i="http://www.w3.org/2001/XMLSchema-instance">','<request i:type="a:RetrieveEntityRequest" ','xmlns:a="http://schemas.microsoft.com/xrm/2011/Contracts">','<a:Parameters xmlns:b="http://schemas.datacontract.org/2004/07/System.Collections.Generic">',"<a:KeyValuePairOfstringanyType>","<b:key>EntityFilters</b:key>",'<b:value i:type="c:EntityFilters" xmlns:c="http://schemas.microsoft.com/xrm/2011/Metadata">',h(a),"</b:value>","</a:KeyValuePairOfstringanyType>","<a:KeyValuePairOfstringanyType>","<b:key>MetadataId</b:key>",'<b:value i:type="ser:guid" xmlns:ser="http://schemas.microsoft.com/2003/10/Serialization/">',"00000000-0000-0000-0000-000000000000","</b:value>","</a:KeyValuePairOfstringanyType>","<a:KeyValuePairOfstringanyType>","<b:key>RetrieveAsIfPublished</b:key>",'<b:value i:type="c:boolean" xmlns:c="http://www.w3.org/2001/XMLSchema">',!!d,"</b:value>","</a:KeyValuePairOfstringanyType>","<a:KeyValuePairOfstringanyType>","<b:key>LogicalName</b:key>",'<b:value i:type="c:string" xmlns:c="http://www.w3.org/2001/XMLSchema">',c,"</b:value>","</a:KeyValuePairOfstringanyType>","</a:Parameters>",'<a:RequestId i:nil="true" />',"<a:RequestName>RetrieveEntity</a:RequestName>","</request>","</Execute>"];
return b(e.join(""))}function ia(b,e,f,g){var h,j,k,l,m=i(b),n=a.Cache.get("Metadata.Entities",e);return n&&(n.FilterLevel&m)===m?g?new a.Promise.Promise(function(a){
a(n.Metadata)}):d(n.Metadata,!0):(h=m,n&&(h=m|n.FilterLevel),j=m,n&&(j=h^n.FilterLevel),k=ha(j,e,f),l=g?da:ea,c(k,l,g,j));
}function ja(a,b,c){return ia(a,b,c,!0)}function ka(a,b,c){return ia(a,b,c,!1)}function la(a,c,d){var e=['<Execute xmlns="http://schemas.microsoft.com/xrm/2011/Contracts/Services" ','xmlns:i="http://www.w3.org/2001/XMLSchema-instance">','<request i:type="a:RetrieveAttributeRequest" ','xmlns:a="http://schemas.microsoft.com/xrm/2011/Contracts">','<a:Parameters xmlns:b="http://schemas.datacontract.org/2004/07/System.Collections.Generic">',"<a:KeyValuePairOfstringanyType>","<b:key>MetadataId</b:key>",'<b:value i:type="ser:guid" xmlns:ser="http://schemas.microsoft.com/2003/10/Serialization/">',"00000000-0000-0000-0000-000000000000","</b:value>","</a:KeyValuePairOfstringanyType>","<a:KeyValuePairOfstringanyType>","<b:key>LogicalName</b:key>",'<b:value i:type="c:string" xmlns:c="http://www.w3.org/2001/XMLSchema">',c,"</b:value>","</a:KeyValuePairOfstringanyType>","<a:KeyValuePairOfstringanyType>","<b:key>EntityLogicalName</b:key>",'<b:value i:type="c:string" xmlns:c="http://www.w3.org/2001/XMLSchema">',a,"</b:value>","</a:KeyValuePairOfstringanyType>","<a:KeyValuePairOfstringanyType>","<b:key>RetrieveAsIfPublished</b:key>",'<b:value i:type="c:boolean" xmlns:c="http://www.w3.org/2001/XMLSchema">',!!d,"</b:value>","</a:KeyValuePairOfstringanyType>","</a:Parameters>",'<a:RequestId i:nil="true" />',"<a:RequestName>RetrieveAttribute</a:RequestName>","</request>","</Execute>"];
return b(e.join(""))}function ma(b,c,d,f){var g,h;4===b.readyState&&(200===b.status?(h=a.Xml.loadXml(b.responseText),g=new V(h.querySelector("ExecuteResult value")),
a.Cache.set("Metadata.Attributes",c,g),d(g)):f(e(b))),b=null}function na(b,c){var f,g,h;return 200===b.status?(g=a.Xml.loadXml(b.responseText),
f=new V(g.querySelector("ExecuteResult value")),a.Cache.set("Metadata.Attributes",c,f),b=null,d(f,!0)):(h=e(b),b=null,d(h,!1));
}function oa(b,e,f,g){var h,i,j=b+"-"+e,k=a.Cache.get("Metadata.Attributes",j);return k?g?new a.Promise.Promise(function(a){
a(k)}):d(k,!0):(h=la(b,e,f),i=g?ma:na,c(h,i,g,j))}function pa(a,b,c){return oa(a,b,c,!0)}function qa(a,b,c){return oa(a,b,c,!1);
}var ra="/XRMServices/2011/Organization.svc/web",sa={xrm:'xmlns:a="http://schemas.microsoft.com/xrm/2011/Contracts"',crm:'xmlns:c="http://schemas.microsoft.com/crm/2011/Contracts"',
collection:'xmlns:b="http://schemas.datacontract.org/2004/07/System.Collections.Generic"',arrays:'xmlns:b="http://schemas.microsoft.com/2003/10/Serialization/Arrays"',
xml:'xmlns:c="http://www.w3.org/2001/XMLSchema"',serialization:'xmlns:c="http://schemas.microsoft.com/2003/10/Serialization/"',
soapenv:'xmlns:soapenv="http://schemas.xmlsoap.org/soap/envelope/"',xmli:'xmlns:i="http://www.w3.org/2001/XMLSchema-instance"'
},ta={All:15,Attributes:3,Default:1,Entity:1,Privileges:5,Relationships:9};return{entityFilters:ta,getEntityByTypeCode:W,
getTypeCodeByEntity:X,getTypeCodeByEntitySync:Y,getOptionSetOption:ba,getOptionSetOptionSync:ca,getOptionSetValue:_,getOptionSetValueSync:aa,
getPrimaryKeyAttribute:Z,getPrimaryKeyAttributeSync:$,retrieveAllEntities:ga,retrieveAttribute:pa,retrieveAttributeSync:qa,
retrieveEntity:ja,retrieveEntitySync:ka,retrieveGlobalOptionSet:C,retrieveGlobalOptionSetSync:D}}(),a.WebAPI=function(){"use strict";
function b(){var b,c=a.Cache.get("WebAPI","ApiVersion");return c?c:(window.Xrm&&Xrm.Page&&Xrm.Page.context&&Xrm.Page.context.getVersion&&(b=Xrm.Page.context.getVersion(),
b=b?b.split(".").slice(0,2).join("."):"8.0"),a.Cache.set("WebAPI","ApiVersion",b||"8.0"),b)}function c(a){var b={Accept:"application/json",
"Content-Type":"application/json; charset=utf-8","OData-MaxVersion":"4.0","OData-Version":"4.0"};if(!a)return b;for(var c in a)a.hasOwnProperty(c)&&void 0===b[c]&&(b[c]=a[c]);
return b}function d(a){if(!a||"string"!=typeof a)return null;try{return JSON.parse(a)}catch(b){return}}function e(b){if(!b||"string"!=typeof b)return null;
try{return a.Xml.loadXml(b)}catch(c){return}}function f(a){var b=d(a.responseText);if(b&&b.error&&b.error.message)return new Error(b.error.message);
var c=e(a.responseText);return c&&c.childNodes&&c.childNodes.length?new Error(c.childNodes[0].textContent):new Error(a.responseText);
}function g(a,b,c){var d,e=a.getResponseHeader("OData-EntityId");e?(d=/\(([^)]+)\)/.exec(e),b(d&&d.length>1?d[1]:e)):c(new Error("Unknown response; Missing 'OData-EntityId' header."));
}function h(a,b,c){var e=d(a.responseText);e?b(e):c(f(a))}function i(a,b){b(!0)}function j(a,b,c){return 204===a.status?i(a,b,c):h(a,b,c);
}function k(a,b,c){return 204===a.status?i(a,b,c):g(a,b,c)}function l(a,b,c,d){if(4===a.readyState){if(200===a.status||204===a.status)b(a,c,d);else{
if(0===a.status)return void(a=null);d(f(a))}a=null}}function m(d){var e,f=a.Promise.defer(),g=new XMLHttpRequest,h={};e=[a.getClientUrl(),"/api/data/v",b(),"/",d.path].join(""),
g.open(d.method,e,!0),h=c(d.headers);for(var i in h)h.hasOwnProperty(i)&&g.setRequestHeader(i,h[i]);return g.onreadystatechange=function(){
l(g,d.parser,f.resolve,f.reject)},g.send(d.data),f.promise}function n(a){return m({data:null,headers:{},method:"DELETE",path:a,
parser:i})}function o(a,b){return m({data:null,headers:b,method:"GET",path:a,parser:h})}function p(a,b,c){return m({data:c,
headers:b,method:"PATCH",path:a,parser:k})}function q(a,b,c,d){return m({data:c,headers:b,method:"POST",path:a,parser:d});
}function r(b,c){return b?c?q(b,{},JSON.stringify(c),g):a.Promise.reject(new Error("'entity' is a required parameter")):a.Promise.reject(new Error("'entitySet' is a required parameter"));
}function s(b,c){if(!b)return a.Promise.reject(new Error("'entitySet' is a required parameter"));if(!c)return a.Promise.reject(new Error("'id' is a required parameter"));
var d=[b,"(",c,")"].join("");return n(d)}function t(b,c){if(!b)return a.Promise.reject(new Error("'entitySet' is a required parameter"));
if(!c)return a.Promise.reject(new Error("'fetchXml' is a required parameter"));var d=encodeURIComponent(c),e=[b,"?fetchXml=",d].join("");
return o(e,{Prefer:'odata.include-annotations="*"'})}function u(b){return b?o(b,{}):a.Promise.reject(new Error("'path' is a required parameter"));
}function v(b,c){return b?c?q(b,{},JSON.stringify(c),j):a.Promise.reject(new Error("'data' is a required parameter")):a.Promise.reject(new Error("'path' is a required parameter"));
}function w(b,c){if(!b)return a.Promise.reject(new Error("'entitySet' is a required parameter"));var d=[b,"?",c||""].join("");
return o(d,{Prefer:'odata.include-annotations="*"'})}function x(a,b){throw new Error("Not Implemented.")}function y(b,c,d){
if(!b)return a.Promise.reject(new Error("'entitySet' is a required parameter"));if(!c)return a.Promise.reject(new Error("'id' is a required parameter"));
var e="string"==typeof c?[b,"(",c,")"].join(""):b;return p(e,{},JSON.stringify(d))}return{create:r,destroy:s,fetch:t,get:u,
query:w,queryAll:x,post:v,upsert:y}}(),a.User=function(){function b(){var a=Array.prototype.concat.apply([],arguments);return f(!1).then(function(b){
if(b)return g(a,b)})}function c(){var a=Array.prototype.concat.apply([],arguments),b=f(!0);return!!b&&g(a,b)}function d(){
return f(!1)}function e(){return f(!0)}function f(b){var c,d,e="",f=[],g=j();return g?(a.each(g,function(b,c){h("Roles",a.Guid.format(c))||(e+='<condition attribute="roleid" operator="eq" value="'+c+'" />');
}),c=['<fetch mapping="logical" version="1.0">','<entity name="role">','<attribute name="name" />','<attribute name="roleid" />','<filter type="or">',e,"</filter>","</entity>","</fetch>"].join(""),
b?(d=a.OrgService.retrieveMultipleSync(c),a.each(d.Value.Entities,function(b,c){i("Roles",a.Guid.format(c.roleid.Value),c.name);
}),a.each(g,function(b,c){f.push(h("Roles",a.Guid.format(c)))}),f):a.OrgService.retrieveMultiple(c).then(function(b){return a.each(b.Entities,function(b,c){
i("Roles",a.Guid.format(c.roleid.Value),c.name)}),a.each(g,function(b,c){f.push(h("Roles",a.Guid.format(c)))}),f})):b?[]:a.Promise.resolve([]);
}function g(b,c){var d;for(d=0;d<b.length;d++)if(a.Array.indexOf(c,b[d])>-1)return!0;return!1}function h(b,c){var d=a.Cache.get(q,b)||{};
return d[c]||null}function i(b,c,d){var e=a.Cache.get(q,b)||{};e[c]=d,a.Cache.set(q,b,e)}function j(){var a=null;return window.Xrm&&Xrm.Page&&Xrm.Page.context?a=Xrm.Page.context.getUserRoles():window.GetGlobalContext&&(a=GetGlobalContext().getUserRoles()),
a?a:void alert("Unable to determine the user's roles from Xrm.Page.context.  Please include ClientGlobalContext.js.aspx.");
}function k(){return o(!1)}function l(){return o(!0)}function m(){var b,c=Array.prototype.concat.apply([],arguments);return o(!1).then(function(d){
return!!d&&(b=[],a.each(d,function(a,c){b.push(c.name)}),g(c,b))})}function n(){var b=Array.prototype.concat.apply([],arguments),c=o(!0),d=[];
return!!c&&(a.each(c,function(a,b){d.push(b.name)}),g(b,d))}function o(b){var c,d,e=a.Cache.get(q,"Teams");return b&&e?e.slice(0):b?(e=[],
c=p(),d=a.OrgService.retrieveMultipleSync(c),d.Success&&d.Value.Entities&&0!==d.Value.Entities.length?(a.each(d.Value.Entities,function(b,c){
e.push({name:c.name,id:a.Guid.format(c.teamid.Value)})}),a.Cache.set(q,"Teams",e),e.slice(0)):[]):e?a.Promise.resolve(e.slice(0)):(e=[],
c=p(),a.OrgService.retrieveMultiple(c).then(function(b){return b.Entities&&0!==b.Entities.length?(a.each(b.Entities,function(b,c){
e.push({name:c.name,id:a.Guid.format(c.teamid.Value)})}),a.Cache.set(q,"Teams",e),e.slice(0)):[]}))}function p(){return['<fetch mapping="logical" version="1.0">','<entity name="team">','<attribute name="name" />','<attribute name="teamid" />','<link-entity name="teammembership" from="teamid" to="teamid">',"<filter>",'<condition attribute="systemuserid" operator="eq-userid" />',"</filter>","</link-entity>","</entity>","</fetch>"].join("");
}var q="Sonoma.User";return{getRoles:d,getRolesSync:e,hasRole:b,hasRoleSync:c,getTeams:k,getTeamsSync:l,belongsToTeam:m,belongsToTeamSync:n
}}(),this.Sonoma=a}).call(this);