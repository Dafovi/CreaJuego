mergeInto(LibraryManager.library,{
  CreaJuegoPickImage:function(target){
    var receiver=UTF8ToString(target),input=document.createElement('input');input.type='file';input.accept='image/png,image/jpeg';
    input.onchange=function(){if(!input.files.length)return;var reader=new FileReader();reader.onload=function(){SendMessage(receiver,'ReceiveImageDataUrl',reader.result);};reader.readAsDataURL(input.files[0]);};
    input.click();
  }
});