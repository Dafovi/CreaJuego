mergeInto(LibraryManager.library,{
  CreaJuegoPickImages:function(target,maxInputBytes,maxOutputBytes,maxDimension){
    var receiver=UTF8ToString(target),input=document.createElement('input');
    input.type='file';input.multiple=true;input.accept='.png,.jpg,.jpeg,image/png,image/jpeg';
    input.onchange=function(){
      var files=Array.prototype.slice.call(input.files||[]),index=0;
      function fail(message){SendMessage(receiver,'ReceiveImageError',message);}
      function next(){
        if(index>=files.length){SendMessage(receiver,'ReceiveImageBatchComplete',String(files.length));return;}
        var file=files[index++],name=(file.name||'Mi imagen'),lower=name.toLowerCase();
        if(!(/\.(png|jpe?g)$/.test(lower))||!(/image\/(png|jpeg)/.test(file.type))){fail(name+': elige una imagen PNG o JPG.');next();return;}
        if(file.size>maxInputBytes){fail(name+': la imagen original es demasiado grande.');next();return;}
        var reader=new FileReader();
        reader.onerror=function(){fail(name+': no se pudo leer esta imagen.');next();};
        reader.onload=function(){
          var image=new Image();
          image.onerror=function(){fail(name+': no se pudo leer esta imagen.');next();};
          image.onload=function(){
            var ratio=Math.min(1,maxDimension/Math.max(image.naturalWidth,image.naturalHeight));
            var width=Math.max(1,Math.round(image.naturalWidth*ratio)),height=Math.max(1,Math.round(image.naturalHeight*ratio));
            var isPng=file.type==='image/png'||/\.png$/.test(lower),result='';
            for(var attempt=0;attempt<6;attempt++){
              var canvas=document.createElement('canvas');canvas.width=width;canvas.height=height;
              var context=canvas.getContext('2d');context.imageSmoothingEnabled=true;context.imageSmoothingQuality='high';context.drawImage(image,0,0,width,height);
              result=canvas.toDataURL(isPng?'image/png':'image/jpeg',.84);
              var encoded=result.substring(result.indexOf(',')+1),bytes=Math.ceil(encoded.length*3/4);
              if(bytes<=maxOutputBytes)break;width=Math.max(128,Math.round(width*.78));height=Math.max(128,Math.round(height*.78));
            }
            var finalBytes=Math.ceil(result.substring(result.indexOf(',')+1).length*3/4);
            if(finalBytes>maxOutputBytes){fail(name+': no se pudo reducir lo suficiente. Prueba con una imagen más sencilla.');next();return;}
            SendMessage(receiver,'ReceiveImageImport',JSON.stringify({name:name.replace(/\.[^.]+$/,''),dataUrl:result}));next();
          };
          image.src=reader.result;
        };
        reader.readAsDataURL(file);
      }
      next();
    };
    input.click();
  },
  CreaJuegoOpenImageEditor:function(target,modePtr,imageNamePtr,dataUrlPtr,settingsPtr,maxInputBytes,maxOutputBytes,maxDimension){
    var receiver=UTF8ToString(target),mode=UTF8ToString(modePtr),initialName=UTF8ToString(imageNamePtr)||'Mi imagen',initialData=UTF8ToString(dataUrlPtr)||'',initialSettings=null;
    try{var settingsText=UTF8ToString(settingsPtr)||'';if(settingsText)initialSettings=JSON.parse(settingsText);}catch(ignored){}
    var overlay=null,stream=null,working=null,crop=null,solid=!!(initialSettings&&initialSettings.paintEnabled),solidColor=initialSettings&&initialSettings.paintColor||'#4f8cff',removeBackground=!!(initialSettings&&initialSettings.removeBackground),backgroundColor=initialSettings&&initialSettings.backgroundColor||'#ffffff',backgroundTolerance=initialSettings&&initialSettings.backgroundTolerance>=0?initialSettings.backgroundTolerance:18;
    function send(method,value){SendMessage(receiver,method,value||'');}
    function stopCamera(){if(stream){stream.getTracks().forEach(function(track){track.stop();});stream=null;}}
    function close(){stopCamera();if(overlay&&overlay.parentNode)overlay.parentNode.removeChild(overlay);overlay=null;}
    function cancel(){close();send('ReceiveImageBatchComplete','0');}
    function fail(message){send('ReceiveImageError',message);cancel();}
    function bytes(data){return Math.ceil(data.substring(data.indexOf(',')+1).length*3/4);}
    function applyEffects(context,width,height){
      if(!removeBackground&&!solid)return;var pixels=context.getImageData(0,0,width,height),paint=/^#(..)(..)(..)$/.exec(solidColor)||['','4f','8c','ff'],background=/^#(..)(..)(..)$/.exec(backgroundColor)||['','ff','ff','ff'],br=parseInt(background[1],16),bg=parseInt(background[2],16),bb=parseInt(background[3],16),backgroundLimit=backgroundTolerance*2.2;
      for(var i=0;i<pixels.data.length;i+=4){if(removeBackground){var dr=pixels.data[i]-br,dg=pixels.data[i+1]-bg,db=pixels.data[i+2]-bb;if(Math.sqrt(dr*dr+dg*dg+db*db)<=backgroundLimit)pixels.data[i+3]=0;}if(solid&&pixels.data[i+3]>0){pixels.data[i]=parseInt(paint[1],16);pixels.data[i+1]=parseInt(paint[2],16);pixels.data[i+2]=parseInt(paint[3],16);}}context.putImageData(pixels,0,0);
    }
    function compactSource(){
      var width=working.width,height=working.height,result='';for(var attempt=0;attempt<8;attempt++){var source=document.createElement('canvas');source.width=width;source.height=height;source.getContext('2d').drawImage(working,0,0,width,height);result=source.toDataURL('image/png');if(bytes(result)<=maxOutputBytes)break;width=Math.max(64,Math.round(width*.78));height=Math.max(64,Math.round(height*.78));}return result;
    }
    function button(text,primary){var b=document.createElement('button');b.textContent=text;b.style.cssText='border:1px solid #56708e;border-radius:6px;padding:10px 14px;background:'+(primary?'#147d43':'#25364c')+';color:#fff;font:600 14px Arial;cursor:pointer;';return b;}
    function basePanel(title){
      overlay=document.createElement('div');overlay.style.cssText='position:fixed;inset:0;z-index:2147483646;background:rgba(4,10,20,.86);display:flex;align-items:center;justify-content:center;padding:16px;box-sizing:border-box;';
      var panel=document.createElement('div');panel.style.cssText='width:min(920px,96vw);max-height:94vh;overflow:auto;background:#162235;color:#f4f7fb;border:1px solid #58718e;border-radius:12px;padding:18px;box-sizing:border-box;box-shadow:0 18px 60px rgba(0,0,0,.55);font-family:Arial,sans-serif;';
      var heading=document.createElement('div');heading.textContent=title;heading.style.cssText='font-size:22px;font-weight:700;margin-bottom:12px;color:#86b7ff;';panel.appendChild(heading);overlay.appendChild(panel);document.body.appendChild(overlay);return panel;
    }
    function readFile(file){
      if(!file)return;if(file.size>maxInputBytes){fail('La imagen original es demasiado grande.');return;}
      if(file.type&&!/^image\/(png|jpeg)$/.test(file.type)){fail('Elige una imagen PNG o JPG.');return;}
      initialName=(file.name||initialName).replace(/\.[^.]+$/,'');var reader=new FileReader();reader.onerror=function(){fail('No se pudo leer esta imagen.');};reader.onload=function(){loadImage(reader.result);};reader.readAsDataURL(file);
    }
    function chooseFile(cameraHint){
      var input=document.createElement('input');input.type='file';input.accept='.png,.jpg,.jpeg,image/png,image/jpeg';if(cameraHint)input.setAttribute('capture','environment');
      input.onchange=function(){readFile(input.files&&input.files[0]);};input.click();
    }
    function loadImage(source){
      var image=new Image();image.onerror=function(){fail('No se pudo leer esta imagen.');};image.onload=function(){
        var ratio=Math.min(1,maxDimension/Math.max(image.naturalWidth,image.naturalHeight));working=document.createElement('canvas');working.width=Math.max(1,Math.round(image.naturalWidth*ratio));working.height=Math.max(1,Math.round(image.naturalHeight*ratio));var workingContext=working.getContext('2d');workingContext.imageSmoothingEnabled=true;workingContext.imageSmoothingQuality='high';workingContext.drawImage(image,0,0,working.width,working.height);showEditor();
      };image.src=source;
    }
    function rotate(){
      var next=document.createElement('canvas');next.width=working.height;next.height=working.width;var context=next.getContext('2d');context.translate(next.width/2,next.height/2);context.rotate(Math.PI/2);context.drawImage(working,-working.width/2,-working.height/2);working=next;resetCrop();draw();
    }
    function resetCrop(){var inset=Math.round(Math.min(working.width,working.height)*.06);crop={x:inset,y:inset,w:Math.max(1,working.width-inset*2),h:Math.max(1,working.height-inset*2)};}
    function showEditor(){
      close();var panel=basePanel('Prepara tu imagen');var help=document.createElement('div');help.textContent='Arrastra el marco amarillo para elegir qué parte conservar. Los cambios de color y fondo se muestran antes de guardar.';help.style.cssText='color:#b9c7da;font-size:14px;margin-bottom:10px;';panel.appendChild(help);
      var canvas=document.createElement('canvas');canvas.width=860;canvas.height=500;canvas.style.cssText='display:block;width:100%;height:min(55vh,500px);background:#0b1320;border-radius:8px;touch-action:none;';panel.appendChild(canvas);var context=canvas.getContext('2d'),view={x:0,y:0,s:1},preview=document.createElement('canvas');resetCrop();
      var drag=null;
      function point(event){var rect=canvas.getBoundingClientRect();return{x:((event.clientX-rect.left)*canvas.width/rect.width-view.x)/view.s,y:((event.clientY-rect.top)*canvas.height/rect.height-view.y)/view.s};}
      function limit(){var min=Math.max(8,Math.min(working.width,working.height)*.03);crop.w=Math.max(min,Math.min(crop.w,working.width-crop.x));crop.h=Math.max(min,Math.min(crop.h,working.height-crop.y));crop.x=Math.max(0,Math.min(crop.x,working.width-crop.w));crop.y=Math.max(0,Math.min(crop.y,working.height-crop.h));}
      if(initialSettings&&initialSettings.cropWidth>0&&initialSettings.cropHeight>0){crop={x:(initialSettings.cropX||0)*working.width,y:(initialSettings.cropY||0)*working.height,w:initialSettings.cropWidth*working.width,h:initialSettings.cropHeight*working.height};limit();}
      function drawEditor(){
        context.clearRect(0,0,canvas.width,canvas.height);view.s=Math.min(canvas.width/working.width,canvas.height/working.height);view.x=(canvas.width-working.width*view.s)/2;view.y=(canvas.height-working.height*view.s)/2;
        var tile=12;for(var cy=view.y;cy<view.y+working.height*view.s;cy+=tile)for(var cx=view.x;cx<view.x+working.width*view.s;cx+=tile){context.fillStyle=((Math.floor((cx-view.x)/tile)+Math.floor((cy-view.y)/tile))%2)?'#6b7480':'#aab2bc';context.fillRect(cx,cy,tile,tile);}context.drawImage(preview,view.x,view.y,working.width*view.s,working.height*view.s);context.fillStyle='rgba(2,8,18,.62)';context.beginPath();context.rect(view.x,view.y,working.width*view.s,working.height*view.s);context.rect(view.x+crop.x*view.s,view.y+crop.y*view.s,crop.w*view.s,crop.h*view.s);context.fill('evenodd');
        context.strokeStyle='#ffd43b';context.lineWidth=3;context.strokeRect(view.x+crop.x*view.s,view.y+crop.y*view.s,crop.w*view.s,crop.h*view.s);context.fillStyle='#ffd43b';[[crop.x,crop.y],[crop.x+crop.w,crop.y],[crop.x,crop.y+crop.h],[crop.x+crop.w,crop.y+crop.h]].forEach(function(p){context.fillRect(view.x+p[0]*view.s-6,view.y+p[1]*view.s-6,12,12);});
      }
      function refreshPreview(){preview.width=working.width;preview.height=working.height;var previewContext=preview.getContext('2d');previewContext.drawImage(working,0,0);applyEffects(previewContext,preview.width,preview.height);drawEditor();}
      draw=refreshPreview;refreshPreview();
      canvas.onpointerdown=function(event){var p=point(event),threshold=18/view.s,corners=[[crop.x,crop.y,'nw'],[crop.x+crop.w,crop.y,'ne'],[crop.x,crop.y+crop.h,'sw'],[crop.x+crop.w,crop.y+crop.h,'se']],kind='move';for(var i=0;i<corners.length;i++)if(Math.abs(p.x-corners[i][0])<threshold&&Math.abs(p.y-corners[i][1])<threshold){kind=corners[i][2];break;}if(kind==='move'&&(p.x<crop.x||p.x>crop.x+crop.w||p.y<crop.y||p.y>crop.y+crop.h))return;drag={kind:kind,x:p.x,y:p.y,crop:{x:crop.x,y:crop.y,w:crop.w,h:crop.h}};canvas.setPointerCapture(event.pointerId);};
      canvas.onpointermove=function(event){if(!drag)return;var p=point(event),dx=p.x-drag.x,dy=p.y-drag.y,r=drag.crop;crop={x:r.x,y:r.y,w:r.w,h:r.h};if(drag.kind==='move'){crop.x=r.x+dx;crop.y=r.y+dy;}else{if(drag.kind.indexOf('w')>=0){crop.x=r.x+dx;crop.w=r.w-dx;}if(drag.kind.indexOf('e')>=0)crop.w=r.w+dx;if(drag.kind.indexOf('n')>=0){crop.y=r.y+dy;crop.h=r.h-dy;}if(drag.kind.indexOf('s')>=0)crop.h=r.h+dy;}limit();drawEditor();};canvas.onpointerup=canvas.onpointercancel=function(){drag=null;};
      var tools=document.createElement('div');tools.style.cssText='display:flex;gap:9px;align-items:center;flex-wrap:wrap;margin-top:12px;';panel.appendChild(tools);var rotateButton=button('Girar 90°',false);rotateButton.onclick=rotate;tools.appendChild(rotateButton);
      var colorLabel=document.createElement('label');colorLabel.style.cssText='display:flex;gap:7px;align-items:center;color:#e8eef7;font-size:14px;';var colorToggle=document.createElement('input');colorToggle.type='checkbox';colorToggle.checked=solid;colorToggle.onchange=function(){solid=colorToggle.checked;refreshPreview();};var color=document.createElement('input');color.type='color';color.value=solidColor;color.oninput=function(){solidColor=color.value;refreshPreview();};colorLabel.appendChild(colorToggle);colorLabel.appendChild(document.createTextNode('Pintar con un color'));colorLabel.appendChild(color);tools.appendChild(colorLabel);
      var backgroundLabel=document.createElement('label');backgroundLabel.style.cssText='display:flex;gap:7px;align-items:center;color:#e8eef7;font-size:14px;';var backgroundToggle=document.createElement('input');backgroundToggle.type='checkbox';backgroundToggle.checked=removeBackground;backgroundToggle.onchange=function(){removeBackground=backgroundToggle.checked;refreshPreview();};var backgroundPicker=document.createElement('input');backgroundPicker.type='color';backgroundPicker.value=backgroundColor;backgroundPicker.oninput=function(){backgroundColor=backgroundPicker.value;refreshPreview();};var tolerance=document.createElement('input');tolerance.type='range';tolerance.min='0';tolerance.max='100';tolerance.value=String(backgroundTolerance);tolerance.title='Tolerancia del fondo';tolerance.oninput=function(){backgroundTolerance=parseInt(tolerance.value,10)||0;refreshPreview();};backgroundLabel.appendChild(backgroundToggle);backgroundLabel.appendChild(document.createTextNode('Quitar fondo'));backgroundLabel.appendChild(backgroundPicker);backgroundLabel.appendChild(tolerance);tools.appendChild(backgroundLabel);
      var resetButton=button('Restablecer todo',false);resetButton.onclick=function(){solid=false;solidColor='#4f8cff';removeBackground=false;backgroundColor='#ffffff';backgroundTolerance=18;colorToggle.checked=false;color.value=solidColor;backgroundToggle.checked=false;backgroundPicker.value=backgroundColor;tolerance.value=String(backgroundTolerance);resetCrop();refreshPreview();};tools.appendChild(resetButton);
      var name=document.createElement('input');name.value=initialName;name.maxLength=48;name.setAttribute('aria-label','Nombre de la imagen');name.style.cssText='flex:1;min-width:180px;border:1px solid #56708e;border-radius:6px;padding:10px;background:#0f1929;color:#fff;font:14px Arial;';tools.appendChild(name);
      var actions=document.createElement('div');actions.style.cssText='display:flex;justify-content:flex-end;gap:10px;margin-top:14px;';panel.appendChild(actions);var cancelButton=button('Cancelar',false);cancelButton.onclick=cancel;actions.appendChild(cancelButton);var saveButton=button('Guardar en Mi biblioteca',true);saveButton.onclick=function(){
        limit();var ratio=Math.min(1,maxDimension/Math.max(crop.w,crop.h)),width=Math.max(1,Math.round(crop.w*ratio)),height=Math.max(1,Math.round(crop.h*ratio)),result='';
        for(var attempt=0;attempt<8;attempt++){var output=document.createElement('canvas');output.width=width;output.height=height;var outputContext=output.getContext('2d');outputContext.imageSmoothingEnabled=true;outputContext.imageSmoothingQuality='high';outputContext.drawImage(working,crop.x,crop.y,crop.w,crop.h,0,0,width,height);applyEffects(outputContext,width,height);result=output.toDataURL('image/png');if(bytes(result)<=maxOutputBytes)break;width=Math.max(64,Math.round(width*.78));height=Math.max(64,Math.round(height*.78));}
        var original=compactSource();if(bytes(result)>maxOutputBytes||bytes(original)>maxOutputBytes){send('ReceiveImageError','No se pudo reducir lo suficiente. Prueba con un recorte más pequeño.');return;}send('ReceiveImageImport',JSON.stringify({name:(name.value||'Mi imagen').trim(),dataUrl:result,originalDataUrl:original,cropX:crop.x/working.width,cropY:crop.y/working.height,cropWidth:crop.w/working.width,cropHeight:crop.h/working.height,paintEnabled:solid,paintColor:solidColor,removeBackground:removeBackground,backgroundColor:backgroundColor,backgroundTolerance:backgroundTolerance}));close();send('ReceiveImageBatchComplete','1');
      };actions.appendChild(saveButton);
    }
    var draw=function(){};
    function showCamera(){
      var panel=basePanel('Tomar una foto');var text=document.createElement('div');text.textContent='Permite el uso de la cámara y encuadra tu dibujo u objeto.';text.style.cssText='color:#b9c7da;margin-bottom:10px;';panel.appendChild(text);var video=document.createElement('video');video.autoplay=true;video.playsInline=true;video.muted=true;video.style.cssText='display:block;width:100%;max-height:65vh;background:#050a12;border-radius:8px;';panel.appendChild(video);var actions=document.createElement('div');actions.style.cssText='display:flex;justify-content:flex-end;gap:10px;margin-top:12px;';panel.appendChild(actions);var fileButton=button('Elegir archivo',false);fileButton.onclick=function(){close();chooseFile(true);};actions.appendChild(fileButton);var cancelButton=button('Cancelar',false);cancelButton.onclick=cancel;actions.appendChild(cancelButton);var shot=button('Tomar foto',true);shot.disabled=true;shot.onclick=function(){var capture=document.createElement('canvas');capture.width=video.videoWidth;capture.height=video.videoHeight;capture.getContext('2d').drawImage(video,0,0);initialName='Foto';stopCamera();loadImage(capture.toDataURL('image/jpeg',.9));};actions.appendChild(shot);
      if(!navigator.mediaDevices||!navigator.mediaDevices.getUserMedia){text.textContent='La cámara no está disponible aquí. Puedes elegir una foto guardada.';return;}navigator.mediaDevices.getUserMedia({video:{facingMode:{ideal:'environment'}},audio:false}).then(function(value){stream=value;video.srcObject=value;video.onloadedmetadata=function(){shot.disabled=false;};}).catch(function(){text.textContent='No se pudo abrir la cámara. Revisa el permiso del navegador o elige una foto guardada.';});
    }
    if(mode==='edit'&&initialData)loadImage(initialData);else if(mode==='camera')showCamera();else chooseFile(false);
  }
});
