mergeInto(LibraryManager.library,{
  CreaJuegoDownloadProject:function(filename,json){
    var name=UTF8ToString(filename),contents=UTF8ToString(json),blob=new Blob([contents],{type:'application/json;charset=utf-8'}),url=URL.createObjectURL(blob),link=document.createElement('a');
    link.href=url;link.download=name;document.body.appendChild(link);link.click();document.body.removeChild(link);setTimeout(function(){URL.revokeObjectURL(url);},1000);
  },
  CreaJuegoPickProject:function(target){
    var receiver=UTF8ToString(target),input=document.createElement('input');input.type='file';input.accept='.creajuego,.json,application/json';
    input.onchange=function(){
      if(!input.files.length)return;var file=input.files[0];
      if(file.size>20971520){SendMessage(receiver,'ReceiveProjectError','La copia supera el máximo de 20 MB.');return;}
      var reader=new FileReader();reader.onload=function(){SendMessage(receiver,'ReceiveProjectJson',reader.result);};reader.onerror=function(){SendMessage(receiver,'ReceiveProjectError','No se pudo leer esta copia del nivel.');};reader.readAsText(file);
    };input.click();
  },
  CreaJuegoSyncStorage:function(){if(typeof FS!=='undefined'&&FS.syncfs)FS.syncfs(false,function(error){if(error)console.warn('CreaJuego: no se pudo sincronizar el guardado',error);});}
});
