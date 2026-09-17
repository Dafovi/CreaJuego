mergeInto(LibraryManager.library,{
  CreaJuegoPickImage:function(target,maxBytes,maxDimension){
    var receiver=UTF8ToString(target),input=document.createElement('input');input.type='file';input.accept='.png,.jpg,.jpeg,image/png,image/jpeg';
    input.onchange=function(){
      if(!input.files.length)return;var file=input.files[0],name=file.name.toLowerCase();
      if(!(/\.(png|jpe?g)$/.test(name))||!(/image\/(png|jpeg)/.test(file.type))){SendMessage(receiver,'ReceiveImageError','Elige una imagen PNG o JPG.');return;}
      if(file.size>maxBytes){SendMessage(receiver,'ReceiveImageError','Esta imagen es demasiado grande. Elige una imagen más pequeña.');return;}
      var url=URL.createObjectURL(file),probe=new Image();probe.onload=function(){URL.revokeObjectURL(url);if(probe.naturalWidth>maxDimension||probe.naturalHeight>maxDimension){SendMessage(receiver,'ReceiveImageError','Esta imagen es demasiado grande. Elige una imagen más pequeña.');return;}var reader=new FileReader();reader.onload=function(){SendMessage(receiver,'ReceiveImageDataUrl',reader.result);};reader.readAsDataURL(file);};probe.onerror=function(){URL.revokeObjectURL(url);SendMessage(receiver,'ReceiveImageError','No se pudo leer esta imagen.');};probe.src=url;
    };input.click();
  }
});