"use strict";(self.webpackChunkmidjourney_proxy_admin=self.webpackChunkmidjourney_proxy_admin||[]).push([[971],{66:function(Je,Y,i){i.r(Y),i.d(Y,{default:function(){return De}});var me=i(15009),h=i.n(me),ce=i(99289),F=i.n(ce),pe=i(5574),x=i.n(pe),v=i(67294),fe=i(74981),Qe=i(90252),He=i(22777),e=i(85893),he=function(p){var E=p.value,S=E===void 0?{}:E,a=p.onChange,G=v.useState(JSON.stringify(S,null,2)),L=x()(G,2),I=L[0],Z=L[1],J=v.useState(!0),B=x()(J,2),u=B[0],k=B[1];(0,v.useEffect)(function(){var O=JSON.stringify(S),C=JSON.stringify(JSON.parse(I));O!==C&&Z(JSON.stringify(S,null,2))},[S]);var Q=function(C){Z(C);try{var H=JSON.parse(C);k(!0),a&&a(H)}catch(ae){k(!1)}};return(0,e.jsxs)("div",{style:{width:"100%"},children:[(0,e.jsx)(fe.ZP,{mode:"json",theme:"textmate",value:I,onChange:Q,name:"json-editor",editorProps:{$blockScrolling:!0},height:"auto",maxLines:1/0,setOptions:{showLineNumbers:!0,tabSize:2,useWorker:!1},style:{width:"100%",minHeight:"80px"},fontSize:14,lineHeight:19,showPrintMargin:!0,showGutter:!0,highlightActiveLine:!0}),!u&&(0,e.jsx)("div",{style:{color:"red",marginTop:"8px"},children:"JSON \u683C\u5F0F\u9519\u8BEF\uFF0C\u8BF7\u68C0\u67E5\u8F93\u5165\uFF01"})]})},j=he,M=i(66927),je=function(){var p=document.createElement("div");p.id="full-screen-loading",p.style.position="fixed",p.style.top="0",p.style.left="0",p.style.width="100%",p.style.height="100%",p.style.zIndex="100",p.style.backgroundColor="#fff",p.innerHTML=`
    <style>
      .loading-title {
        font-size: 1.1rem;
        margin-top: 15px;
      }

      .loading-sub-title {
        margin-top: 20px;
        font-size: 1rem;
        color: #888;
      }

      .page-loading-warp {
        display: contents;
        align-items: center;
        justify-content: center;
        padding: 26px;
      }
      .ant-spin {
        position: absolute;
        display: none;
        -webkit-box-sizing: border-box;
        box-sizing: border-box;
        margin: 0;
        padding: 0;
        color: rgba(0, 0, 0, 0.65);
        color: #1890ff;
        font-size: 14px;
        font-variant: tabular-nums;
        line-height: 1.5;
        text-align: center;
        list-style: none;
        opacity: 0;
        -webkit-transition: -webkit-transform 0.3s
          cubic-bezier(0.78, 0.14, 0.15, 0.86);
        transition: -webkit-transform 0.3s
          cubic-bezier(0.78, 0.14, 0.15, 0.86);
        transition: transform 0.3s cubic-bezier(0.78, 0.14, 0.15, 0.86);
        transition: transform 0.3s cubic-bezier(0.78, 0.14, 0.15, 0.86),
          -webkit-transform 0.3s cubic-bezier(0.78, 0.14, 0.15, 0.86);
        -webkit-font-feature-settings: "tnum";
        font-feature-settings: "tnum";
      }

      .ant-spin-spinning {
        position: static;
        display: inline-block;
        opacity: 1;
      }

      .ant-spin-dot {
        position: relative;
        display: inline-block;
        width: 20px;
        height: 20px;
        font-size: 20px;
      }

      .ant-spin-dot-item {
        position: absolute;
        display: block;
        width: 9px;
        height: 9px;
        background-color: #1890ff;
        border-radius: 100%;
        -webkit-transform: scale(0.75);
        -ms-transform: scale(0.75);
        transform: scale(0.75);
        -webkit-transform-origin: 50% 50%;
        -ms-transform-origin: 50% 50%;
        transform-origin: 50% 50%;
        opacity: 0.3;
        -webkit-animation: antspinmove 1s infinite linear alternate;
        animation: antSpinMove 1s infinite linear alternate;
      }

      .ant-spin-dot-item:nth-child(1) {
        top: 0;
        left: 0;
      }

      .ant-spin-dot-item:nth-child(2) {
        top: 0;
        right: 0;
        -webkit-animation-delay: 0.4s;
        animation-delay: 0.4s;
      }

      .ant-spin-dot-item:nth-child(3) {
        right: 0;
        bottom: 0;
        -webkit-animation-delay: 0.8s;
        animation-delay: 0.8s;
      }

      .ant-spin-dot-item:nth-child(4) {
        bottom: 0;
        left: 0;
        -webkit-animation-delay: 1.2s;
        animation-delay: 1.2s;
      }

      .ant-spin-dot-spin {
        -webkit-transform: rotate(45deg);
        -ms-transform: rotate(45deg);
        transform: rotate(45deg);
        -webkit-animation: antrotate 1.2s infinite linear;
        animation: antRotate 1.2s infinite linear;
      }

      .ant-spin-lg .ant-spin-dot {
        width: 32px;
        height: 32px;
        font-size: 32px;
      }

      .ant-spin-lg .ant-spin-dot i {
        width: 14px;
        height: 14px;
      }

      @media all and (-ms-high-contrast: none), (-ms-high-contrast: active) {
        .ant-spin-blur {
          background: #fff;
          opacity: 0.5;
        }
      }

      @-webkit-keyframes antSpinMove {
        to {
          opacity: 1;
        }
      }

      @keyframes antSpinMove {
        to {
          opacity: 1;
        }
      }

      @-webkit-keyframes antRotate {
        to {
          -webkit-transform: rotate(405deg);
          transform: rotate(405deg);
        }
      }

      @keyframes antRotate {
        to {
          -webkit-transform: rotate(405deg);
          transform: rotate(405deg);
        }
      }
      .loading-container {
        display: flex;
        flex-direction: column;
        align-items: center;
        justify-content: center;
        height: 100vh;
        min-height: 362px;
      }
    </style>
    <div class="loading-container">
      <div class="page-loading-warp">
        <div class="ant-spin ant-spin-lg ant-spin-spinning">
          <span class="ant-spin-dot ant-spin-dot-spin">
            <i class="ant-spin-dot-item"></i>
            <i class="ant-spin-dot-item"></i>
            <i class="ant-spin-dot-item"></i>
            <i class="ant-spin-dot-item"></i>
          </span>
        </div>
        <div class="loading-title">
          \u7CFB\u7EDF\u6B63\u5728\u91CD\u542F\u4E2D...
        </div>
        <div class="loading-sub-title">
          \u7CFB\u7EDF\u6B63\u5728\u91CD\u542F\u4E2D\u53EF\u80FD\u9700\u8981\u8F83\u591A\u65F6\u95F4 \u8BF7\u8010\u5FC3\u7B49\u5F85
        </div>
      </div>
    </div>
  `,document.body.appendChild(p)},ve=function(){var p=document.getElementById("full-screen-loading");p&&p.parentNode&&p.parentNode.removeChild(p)},X=i(85060),xe=i(98165),Ze=i(60219),be=i(90930),Me=i(94272),ye=i(71471),s=i(53025),c=i(2453),q=i(28248),Se=i(74330),w=i(42075),U=i(40056),D=i(83622),_=i(83062),b=i(55102),A=i(4393),Ie=i(66309),Ce=i(38703),ee=i(71230),V=i(15746),l=i(72269),o=i(74656),y=i(37804),Te=ye.Z.Text,Fe=function(){var p=s.Z.useForm(),E=x()(p,1),S=E[0],a=(0,Me.useIntl)(),G=(0,v.useState)(!1),L=x()(G,2),I=L[0],Z=L[1],J=(0,v.useState)(null),B=x()(J,2),u=B[0],k=B[1],Q=(0,v.useState)(""),O=x()(Q,2),C=O[0],H=O[1],ae=(0,v.useState)(""),se=x()(ae,2),te=se[0],ke=se[1],Pe=function(){var g=F()(h()().mark(function n(m,d){var r,T;return h()().wrap(function(f){for(;;)switch(f.prev=f.next){case 0:return f.prev=0,Z(!0),r={Host:m,ApiSecret:d},f.next=5,(0,M.gU)(r);case 5:T=f.sent,T.success?c.ZP.success(a.formatMessage({id:"pages.setting.migrateSuccess"})):c.ZP.error(T.message),f.next=12;break;case 9:f.prev=9,f.t0=f.catch(0),c.ZP.error(f.t0);case 12:return f.prev=12,Z(!1),f.finish(12);case 15:case"end":return f.stop()}},n,null,[[0,9,12,15]])}));return function(m,d){return g.apply(this,arguments)}}(),we=function(){C?Pe(C,te):c.ZP.warning(a.formatMessage({id:"pages.setting.migrateTips"}))},Ue=(0,v.useState)(!1),ne=x()(Ue,2),We=ne[0],ie=ne[1],Le=(0,v.useState)(null),re=x()(Le,2),P=re[0],W=re[1],le=function(){P&&clearInterval(P);var n=setInterval(F()(h()().mark(function m(){var d,r;return h()().wrap(function(t){for(;;)switch(t.prev=t.next){case 0:return t.prev=0,t.next=3,(0,M.iE)();case 3:d=t.sent,d.success&&(r=(d==null?void 0:d.data.upgradeInfo)||{},k(r),(r.status==="ReadyToRestart"||r.status==="Failed"||r.status==="Idle"||r.status==="Success")&&(clearInterval(n),W(null))),t.next=10;break;case 7:t.prev=7,t.t0=t.catch(0),console.error("\u76D1\u63A7\u66F4\u65B0\u72B6\u6001\u5931\u8D25:",t.t0);case 10:case"end":return t.stop()}},m,null,[[0,7]])})),2e3);W(n)},Be=function(){var g=F()(h()().mark(function n(){var m;return h()().wrap(function(r){for(;;)switch(r.prev=r.next){case 0:return r.prev=0,ie(!0),r.next=4,(0,M.Py)();case 4:m=r.sent,m.success?(k(m.data),m.data.status==="Downloading"&&le()):c.ZP.error(m.message||"\u68C0\u67E5\u66F4\u65B0\u5931\u8D25"),r.next=11;break;case 8:r.prev=8,r.t0=r.catch(0),c.ZP.error("\u68C0\u67E5\u66F4\u65B0\u5931\u8D25");case 11:return r.prev=11,ie(!1),r.finish(11);case 14:case"end":return r.stop()}},n,null,[[0,8,11,14]])}));return function(){return g.apply(this,arguments)}}(),Oe=function(){var g=F()(h()().mark(function n(){var m,d,r;return h()().wrap(function(t){for(;;)switch(t.prev=t.next){case 0:return t.prev=0,t.next=3,(0,M.Uf)();case 3:if(m=t.sent,!m.success){t.next=19;break}return c.ZP.success("\u5DF2\u53D6\u6D88\u66F4\u65B0"),P&&(clearInterval(P),W(null)),t.prev=7,t.next=10,(0,M.iE)();case 10:d=t.sent,d.success&&(r=(d==null?void 0:d.data.upgradeInfo)||{},k(r)),t.next=17;break;case 14:t.prev=14,t.t0=t.catch(7),console.error("\u76D1\u63A7\u66F4\u65B0\u72B6\u6001\u5931\u8D25:",t.t0);case 17:t.next=20;break;case 19:c.ZP.error(m.message||"\u53D6\u6D88\u66F4\u65B0\u5931\u8D25");case 20:t.next=25;break;case 22:t.prev=22,t.t1=t.catch(0),c.ZP.error("\u53D6\u6D88\u66F4\u65B0\u5931\u8D25");case 25:case"end":return t.stop()}},n,null,[[0,22],[7,14]])}));return function(){return g.apply(this,arguments)}}(),Ke=function(){var g=F()(h()().mark(function n(){return h()().wrap(function(d){for(;;)switch(d.prev=d.next){case 0:q.Z.confirm({title:"\u786E\u8BA4\u91CD\u542F",content:"\u65B0\u7248\u672C ".concat(u==null?void 0:u.latestVersion," \u5DF2\u51C6\u5907\u5C31\u7EEA\uFF0C\u662F\u5426\u7ACB\u5373\u91CD\u542F\u5E94\u7528\u4EE5\u5B8C\u6210\u66F4\u65B0\uFF1F"),okText:"\u7ACB\u5373\u91CD\u542F",cancelText:"\u7A0D\u540E\u91CD\u542F",onOk:function(){var r=F()(h()().mark(function t(){return h()().wrap(function(K){for(;;)switch(K.prev=K.next){case 0:try{(0,M.ur)().then(function(z){Z(!1),z.success?c.ZP.success(z.message||a.formatMessage({id:"pages.setting.restartSuccess"})):c.ZP.error(z.message||a.formatMessage({id:"pages.setting.error"}))})}catch(z){c.ZP.error("\u91CD\u542F\u5931\u8D25")}case 1:case"end":return K.stop()}},t)}));function T(){return r.apply(this,arguments)}return T}()});case 1:case"end":return d.stop()}},n)}));return function(){return g.apply(this,arguments)}}();(0,v.useEffect)(function(){return function(){P&&clearInterval(P)}},[P]);var oe=function(){Z(!0),(0,M.iE)().then(function(n){Z(!1),n.success&&(S.setFieldsValue(n.data),k(n.data.upgradeInfo||{}),n.data.upgradeInfo.status==="Downloading"&&le())})};(0,v.useEffect)(function(){oe()},[]);var Ae=function(){S.validateFields().then(function(n){Z(!0),(0,M.rF)(n).then(function(m){Z(!1),m.success?(c.ZP.success(a.formatMessage({id:"pages.setting.saveSuccess"})),oe()):c.ZP.error(m.message||a.formatMessage({id:"pages.setting.error"}))})}).catch(function(){c.ZP.error(a.formatMessage({id:"pages.setting.error"}))})},Ee=(0,v.useState)(!1),ge=x()(Ee,2),Ne=ge[0],N=ge[1],Re=s.Z.useForm(),ze=x()(Re,1),R=ze[0],Ve=(0,v.useState)(""),de=x()(Ve,2),ue=de[0],$e=de[1],Ge=function(){var g=F()(h()().mark(function n(m){var d,r;return h()().wrap(function(t){for(;;)switch(t.prev=t.next){case 0:return N(!1),je(),d=new Promise(function(f){return setTimeout(f,3e3)}),r=null,t.prev=4,t.next=7,(0,M.df)({password:m});case 7:r=t.sent,r.success?c.ZP.success(r.message):r.code===504&&c.ZP.warning("\u7CFB\u7EDF\u6B63\u5728\u52A0\u8F7D\u4E2D..."),t.next=14;break;case 11:t.prev=11,t.t0=t.catch(4),c.ZP.warning("\u7CFB\u7EDF\u6B63\u5728\u52A0\u8F7D\u4E2D...");case 14:return t.prev=14,t.next=17,d;case 17:return ve(),t.finish(14);case 19:case"end":return t.stop()}},n,null,[[4,11,14,19]])}));return function(m){return g.apply(this,arguments)}}();return(0,e.jsx)(be._z,{children:(0,e.jsx)(s.Z,{form:S,labelAlign:"left",layout:"horizontal",labelCol:{span:6},wrapperCol:{span:18},children:(0,e.jsxs)(Se.Z,{spinning:I,children:[(0,e.jsxs)(w.Z,{style:{marginBottom:"10px",display:"flex",justifyContent:"space-between"},children:[(0,e.jsxs)(w.Z,{children:[(0,e.jsx)(U.Z,{type:"info",style:{paddingTop:"4px",paddingBottom:"4px"},description:a.formatMessage({id:"pages.setting.tips"})}),(0,e.jsx)(D.ZP,{danger:!0,type:"dashed",icon:(0,e.jsx)(X.Z,{}),onClick:Be,loading:I,children:a.formatMessage({id:"pages.setting.checkUpdate"})})]}),(0,e.jsxs)(w.Z,{children:[(0,e.jsx)(_.Z,{placement:"bottom",title:(0,e.jsxs)("div",{style:{display:"flex",flexDirection:"column",gap:8,padding:8},children:[(0,e.jsx)(b.Z,{style:{marginBottom:8},placeholder:"mjplus host",value:C,onChange:function(n){return H(n.target.value)}}),(0,e.jsx)(b.Z,{placeholder:"mj-api-secret",value:te,onChange:function(n){return ke(n.target.value)}})]}),children:(0,e.jsx)(D.ZP,{loading:I,type:"primary",ghost:!0,onClick:we,children:a.formatMessage({id:"pages.setting.migrate"})})}),(0,e.jsx)(_.Z,{title:a.formatMessage({id:"pages.setting.restartServiceTips"}),placement:"bottom",children:(0,e.jsx)(D.ZP,{loading:I,icon:(0,e.jsx)(xe.Z,{spin:I}),danger:!0,type:"primary",onClick:function(){return N(!0)},children:a.formatMessage({id:"pages.setting.restart"})})}),(0,e.jsx)(D.ZP,{loading:I,icon:(0,e.jsx)(Ze.Z,{}),type:"primary",onClick:Ae,children:a.formatMessage({id:"pages.setting.save"})}),(0,e.jsx)(q.Z,{open:Ne,onCancel:function(){N(!1),R.resetFields()},onOk:function(){R.validateFields().then(function(){Ge(ue),R.resetFields()}).catch(function(n){console.log("Validation failed:",n)})},okText:"\u786E\u8BA4",cancelText:"\u53D6\u6D88",children:(0,e.jsx)(s.Z,{form:R,layout:"vertical",initialValues:{remember:!0},children:(0,e.jsx)(s.Z.Item,{label:a.formatMessage({id:"pages.setting.restartModalTip"}),name:"password",rules:[{required:!0,message:a.formatMessage({id:"pages.setting.restartModalTip"})}],children:(0,e.jsx)(b.Z.Password,{placeholder:a.formatMessage({id:"pages.setting.restartModalTip"}),value:ue,onChange:function(n){$e(n.target.value),n.target.value.trim()!==""&&S.validateFields(["password"])}})})})})]})]}),u&&(0,e.jsx)(A.Z,{style:{marginTop:16,marginBottom:16},size:"small",title:(0,e.jsxs)(w.Z,{children:[(0,e.jsx)(X.Z,{}),a.formatMessage({id:"pages.setting.updateStatus"})]}),children:(0,e.jsxs)(w.Z,{direction:"vertical",style:{width:"100%"},children:[u.hasUpdate&&(0,e.jsx)(Ie.Z,{color:"green",children:u.latestVersion}),u.message&&(0,e.jsx)("div",{children:(0,e.jsx)(Te,{type:"secondary",children:u.message})}),u.status==="Downloading"&&(0,e.jsxs)("div",{children:[(0,e.jsx)(Ce.Z,{percent:u.progress,status:"active",format:function(n){return"".concat(n,"%")}}),(0,e.jsx)(D.ZP,{size:"small",danger:!0,onClick:Oe,style:{marginTop:8},children:"\u53D6\u6D88\u4E0B\u8F7D"})]}),u.status==="ReadyToRestart"&&(0,e.jsx)(U.Z,{message:"\u65B0\u7248\u672C ".concat(u.latestVersion," \u5DF2\u51C6\u5907\u5C31\u7EEA"),description:"\u70B9\u51FB\u4E0B\u65B9\u6309\u94AE\u91CD\u542F\u5E94\u7528\u4EE5\u5B8C\u6210\u66F4\u65B0",type:"success",showIcon:!0,action:(0,e.jsx)(D.ZP,{type:"primary",onClick:function(){return N(!0)},children:"\u91CD\u542F\u5E94\u7528"})}),u.status==="Failed"&&u.errorMessage&&(0,e.jsx)(U.Z,{message:"\u66F4\u65B0\u5931\u8D25",description:u.errorMessage,type:"error",showIcon:!0}),!u.hasUpdate&&u.status==="Success"&&(0,e.jsx)(U.Z,{message:"\u5DF2\u662F\u6700\u65B0\u7248\u672C",description:"\u5F53\u524D\u7248\u672C\u5DF2\u662F\u6700\u65B0\u7248\u672C",type:"info",showIcon:!0}),!u.supportedPlatform&&(0,e.jsx)(U.Z,{message:"\u5F53\u524D\u5E73\u53F0\u4E0D\u652F\u6301\u81EA\u52A8\u66F4\u65B0",description:"\u68C0\u6D4B\u5230\u5E73\u53F0: ".concat(u.platform),type:"warning",showIcon:!0})]})}),(0,e.jsxs)(ee.Z,{gutter:16,children:[(0,e.jsx)(V.Z,{span:12,children:(0,e.jsxs)(A.Z,{title:a.formatMessage({id:"pages.setting.accountSetting"}),children:[(0,e.jsx)(s.Z.Item,{label:a.formatMessage({id:"pages.setting.enableSwagger"}),name:"enableSwagger",extra:S.getFieldValue("enableSwagger")?(0,e.jsx)("a",{href:"/swagger",target:"_blank",rel:"noreferrer",children:a.formatMessage({id:"pages.setting.swaggerLink"})}):"",children:(0,e.jsx)(l.Z,{})}),(0,e.jsx)(s.Z.Item,{label:a.formatMessage({id:"pages.setting.databaseType"}),name:"databaseType",children:(0,e.jsxs)(o.Z,{allowClear:!0,children:[(0,e.jsx)(o.Z.Option,{value:"LiteDB",children:"LiteDB"}),(0,e.jsx)(o.Z.Option,{value:"MongoDB",children:"MongoDB"}),(0,e.jsx)(o.Z.Option,{value:"SQLite",children:"SQLite"}),(0,e.jsx)(o.Z.Option,{value:"MySQL",children:"MySQL"}),(0,e.jsx)(o.Z.Option,{value:"PostgreSQL",children:"PostgreSQL"}),(0,e.jsx)(o.Z.Option,{value:"SQLServer",children:"SQLServer"})]})}),(0,e.jsx)(s.Z.Item,{label:a.formatMessage({id:"pages.setting.databaseConnectionString"}),name:"databaseConnectionString",extra:(0,e.jsx)(e.Fragment,{children:(0,e.jsx)(D.ZP,{style:{marginTop:8},type:"primary",onClick:function(){Z(!0),(0,M.yk)().then(function(n){Z(!1),n.success?c.ZP.success(a.formatMessage({id:"pages.setting.connectSuccess"})):c.ZP.error(n.message||a.formatMessage({id:"pages.setting.connectError"}))})},children:a.formatMessage({id:"pages.setting.testConnect"})})}),children:(0,e.jsx)(b.Z,{})}),(0,e.jsx)(s.Z.Item,{label:a.formatMessage({id:"pages.setting.databaseName"}),name:"databaseName",children:(0,e.jsx)(b.Z,{})}),(0,e.jsx)(s.Z.Item,{label:a.formatMessage({id:"pages.setting.isAutoMigrate"}),name:"isAutoMigrate",tooltip:a.formatMessage({id:"pages.setting.isAutoMigrateTips"}),children:(0,e.jsx)(l.Z,{})}),(0,e.jsx)(s.Z.Item,{label:a.formatMessage({id:"pages.setting.maxCount"}),name:"maxCount",children:(0,e.jsx)(y.Z,{min:-1})}),(0,e.jsx)(s.Z.Item,{label:a.formatMessage({id:"pages.setting.accountChooseRule"}),name:"accountChooseRule",children:(0,e.jsxs)(o.Z,{allowClear:!0,children:[(0,e.jsx)(o.Z.Option,{value:"BestWaitIdle",children:"BestWaitIdle"}),(0,e.jsx)(o.Z.Option,{value:"Random",children:"Random"}),(0,e.jsx)(o.Z.Option,{value:"Weight",children:"Weight"}),(0,e.jsx)(o.Z.Option,{value:"Polling",children:"Polling"})]})}),(0,e.jsx)(s.Z.Item,{label:a.formatMessage({id:"pages.setting.discordConfig"}),name:"ngDiscord",children:(0,e.jsx)(j,{})}),(0,e.jsx)(s.Z.Item,{label:a.formatMessage({id:"pages.setting.proxyConfig"}),name:"proxy",children:(0,e.jsx)(j,{})}),(0,e.jsx)(s.Z.Item,{label:a.formatMessage({id:"pages.setting.imageStorageType"}),name:"imageStorageType",children:(0,e.jsxs)(o.Z,{allowClear:!0,children:[(0,e.jsx)(o.Z.Option,{value:"NONE",children:"NULL"}),(0,e.jsx)(o.Z.Option,{value:"LOCAL",children:"LOCAL"}),(0,e.jsx)(o.Z.Option,{value:"OSS",children:"Aliyun OSS"}),(0,e.jsx)(o.Z.Option,{value:"COS",children:"Tencent COS"}),(0,e.jsx)(o.Z.Option,{value:"R2",children:"Cloudflare R2"})]})}),(0,e.jsx)(s.Z.Item,{label:a.formatMessage({id:"pages.setting.localStorage"}),name:"localStorage",children:(0,e.jsx)(j,{})}),(0,e.jsx)(s.Z.Item,{label:a.formatMessage({id:"pages.setting.aliyunOss"}),name:"aliyunOss",children:(0,e.jsx)(j,{})}),(0,e.jsx)(s.Z.Item,{label:a.formatMessage({id:"pages.setting.tencentCos"}),name:"tencentCos",children:(0,e.jsx)(j,{})}),(0,e.jsx)(s.Z.Item,{label:a.formatMessage({id:"pages.setting.cloudflareR2"}),name:"cloudflareR2",children:(0,e.jsx)(j,{})}),(0,e.jsx)(s.Z.Item,{label:a.formatMessage({id:"pages.setting.replicate"}),name:"replicate",children:(0,e.jsx)(j,{})}),(0,e.jsx)(s.Z.Item,{label:a.formatMessage({id:"pages.setting.translate"}),name:"translateWay",children:(0,e.jsxs)(o.Z,{allowClear:!0,children:[(0,e.jsx)(o.Z.Option,{value:"NULL",children:"NULL"}),(0,e.jsx)(o.Z.Option,{value:"BAIDU",children:"BAIDU"}),(0,e.jsx)(o.Z.Option,{value:"GPT",children:"GPT"})]})}),(0,e.jsx)(s.Z.Item,{label:a.formatMessage({id:"pages.setting.baiduTranslate"}),name:"baiduTranslate",children:(0,e.jsx)(j,{})}),(0,e.jsx)(s.Z.Item,{label:a.formatMessage({id:"pages.setting.openai"}),name:"openai",children:(0,e.jsx)(j,{})}),(0,e.jsx)(s.Z.Item,{label:a.formatMessage({id:"pages.setting.smtp"}),name:"smtp",children:(0,e.jsx)(j,{})}),(0,e.jsx)(s.Z.Item,{label:a.formatMessage({id:"pages.setting.notifyHook"}),name:"notifyHook",children:(0,e.jsx)(b.Z,{})}),(0,e.jsx)(s.Z.Item,{label:a.formatMessage({id:"pages.setting.notifyPoolSize"}),name:"notifyPoolSize",children:(0,e.jsx)(y.Z,{})}),(0,e.jsx)(s.Z.Item,{label:a.formatMessage({id:"pages.setting.captchaServer"}),name:"captchaServer",help:a.formatMessage({id:"pages.setting.captchaServerTip"}),children:(0,e.jsx)(b.Z,{})}),(0,e.jsx)(s.Z.Item,{label:a.formatMessage({id:"pages.setting.captchaNotifyHook"}),name:"captchaNotifyHook",help:a.formatMessage({id:"pages.setting.captchaNotifyHookTip"}),children:(0,e.jsx)(b.Z,{})}),(0,e.jsx)(s.Z.Item,{label:a.formatMessage({id:"pages.setting.captchaNotifySecret"}),name:"captchaNotifySecret",help:a.formatMessage({id:"pages.setting.captchaNotifySecretTip"}),children:(0,e.jsx)(b.Z,{})})]})}),(0,e.jsx)(V.Z,{span:12,children:(0,e.jsxs)(A.Z,{title:a.formatMessage({id:"pages.setting.otherSetting"}),children:[(0,e.jsx)(s.Z.Item,{label:a.formatMessage({id:"pages.setting.enableUpdateCheck"}),name:"enableUpdateCheck",help:a.formatMessage({id:"pages.setting.enableUpdateCheckTips"}),children:(0,e.jsx)(l.Z,{})}),(0,e.jsx)(s.Z.Item,{label:a.formatMessage({id:"pages.setting.licenseKey"}),name:"licenseKey",children:(0,e.jsx)(b.Z,{})}),(0,e.jsx)(s.Z.Item,{label:a.formatMessage({id:"pages.setting.isDemoMode"}),name:"isDemoMode",help:a.formatMessage({id:"pages.setting.isDemoModeTips"}),children:(0,e.jsx)(l.Z,{className:"demo-mode"})}),(0,e.jsx)(s.Z.Item,{label:a.formatMessage({id:"pages.setting.enableAccountSponsor"}),name:"enableAccountSponsor",help:a.formatMessage({id:"pages.setting.enableAccountSponsorTips"}),children:(0,e.jsx)(l.Z,{})}),(0,e.jsx)(s.Z.Item,{label:a.formatMessage({id:"pages.setting.enableOfficial"}),name:"enableOfficial",children:(0,e.jsx)(l.Z,{})}),(0,e.jsx)(s.Z.Item,{label:a.formatMessage({id:"pages.setting.enableYouChuan"}),name:"enableYouChuan",children:(0,e.jsx)(l.Z,{})}),(0,e.jsx)(s.Z.Item,{label:a.formatMessage({id:"pages.setting.isVerticalDomain"}),name:"isVerticalDomain",help:a.formatMessage({id:"pages.setting.isVerticalDomainTips"}),children:(0,e.jsx)(l.Z,{})}),(0,e.jsx)(s.Z.Item,{label:a.formatMessage({id:"pages.setting.enableRegister"}),name:"enableRegister",children:(0,e.jsx)(l.Z,{})}),(0,e.jsx)(s.Z.Item,{label:a.formatMessage({id:"pages.setting.registerUserDefaultDayLimit"}),name:"registerUserDefaultDayLimit",children:(0,e.jsx)(y.Z,{min:-1})}),(0,e.jsx)(s.Z.Item,{label:a.formatMessage({id:"pages.setting.registerUserDefaultTotalLimit"}),name:"registerUserDefaultTotalLimit",children:(0,e.jsx)(y.Z,{min:-1})}),(0,e.jsx)(s.Z.Item,{label:a.formatMessage({id:"pages.setting.registerUserDefaultCoreSize"}),name:"registerUserDefaultCoreSize",children:(0,e.jsx)(y.Z,{min:-1})}),(0,e.jsx)(s.Z.Item,{label:a.formatMessage({id:"pages.setting.registerUserDefaultQueueSize"}),name:"registerUserDefaultQueueSize",children:(0,e.jsx)(y.Z,{min:-1})}),(0,e.jsx)(s.Z.Item,{label:a.formatMessage({id:"pages.setting.enableGuest"}),name:"enableGuest",children:(0,e.jsx)(l.Z,{})}),(0,e.jsx)(s.Z.Item,{label:a.formatMessage({id:"pages.setting.guestDefaultDayLimit"}),name:"guestDefaultDayLimit",children:(0,e.jsx)(y.Z,{min:-1})}),(0,e.jsx)(s.Z.Item,{label:a.formatMessage({id:"pages.setting.guestDefaultCoreSize"}),name:"guestDefaultCoreSize",children:(0,e.jsx)(y.Z,{min:-1})}),(0,e.jsx)(s.Z.Item,{label:a.formatMessage({id:"pages.setting.guestDefaultQueueSize"}),name:"guestDefaultQueueSize",children:(0,e.jsx)(y.Z,{min:-1})}),(0,e.jsx)(s.Z.Item,{label:a.formatMessage({id:"pages.setting.homeDisplayRealIP"}),name:"homeDisplayRealIP",valuePropName:"checked",help:a.formatMessage({id:"pages.setting.homeDisplayRealIP.tooltip"}),children:(0,e.jsx)(l.Z,{})}),(0,e.jsx)(s.Z.Item,{label:a.formatMessage({id:"pages.setting.homeDisplayUserIPState"}),name:"homeDisplayUserIPState",valuePropName:"checked",help:a.formatMessage({id:"pages.setting.homeDisplayUserIPState.tooltip"}),children:(0,e.jsx)(l.Z,{})}),(0,e.jsx)(s.Z.Item,{label:a.formatMessage({id:"pages.setting.homeTopCount"}),name:"homeTopCount",rules:[{type:"number",min:1,max:100,message:a.formatMessage({id:"pages.setting.homeTopCount.range"})}],help:a.formatMessage({id:"pages.setting.homeTopCount.tooltip"}),children:(0,e.jsx)(y.Z,{min:1,max:100,placeholder:a.formatMessage({id:"pages.setting.homeTopCount.placeholder"}),addonAfter:"\u6761"})}),(0,e.jsx)(s.Z.Item,{label:a.formatMessage({id:"pages.setting.consulOptions"}),name:"consulOptions",children:(0,e.jsx)(j,{})}),(0,e.jsx)(s.Z.Item,{label:a.formatMessage({id:"pages.setting.bannedLimiting"}),name:"bannedLimiting",children:(0,e.jsx)(j,{})}),(0,e.jsx)(s.Z.Item,{label:a.formatMessage({id:"pages.setting.ipRateLimiting"}),name:"ipRateLimiting",children:(0,e.jsx)(j,{})}),(0,e.jsx)(s.Z.Item,{label:a.formatMessage({id:"pages.setting.ipBlackRateLimiting"}),name:"ipBlackRateLimiting",children:(0,e.jsx)(j,{})}),(0,e.jsx)(s.Z.Item,{label:a.formatMessage({id:"pages.setting.notify"}),name:"notify",children:(0,e.jsx)(b.Z.TextArea,{autoSize:{minRows:1,maxRows:10}})})]})})]}),(0,e.jsx)(ee.Z,{gutter:16,style:{marginTop:"16px"},children:(0,e.jsx)(V.Z,{span:12,children:(0,e.jsxs)(A.Z,{title:a.formatMessage({id:"pages.setting.discordSetting"}),children:[(0,e.jsx)(s.Z.Item,{label:a.formatMessage({id:"pages.setting.enableAutoGetPrivateId"}),name:"enableAutoGetPrivateId",help:a.formatMessage({id:"pages.setting.enableAutoGetPrivateIdTips"}),children:(0,e.jsx)(l.Z,{})}),(0,e.jsx)(s.Z.Item,{label:a.formatMessage({id:"pages.setting.enableAutoVerifyAccount"}),name:"enableAutoVerifyAccount",help:a.formatMessage({id:"pages.setting.enableAutoVerifyAccountTips"}),children:(0,e.jsx)(l.Z,{})}),(0,e.jsx)(s.Z.Item,{label:a.formatMessage({id:"pages.setting.enableAutoSyncInfoSetting"}),name:"enableAutoSyncInfoSetting",help:a.formatMessage({id:"pages.setting.enableAutoSyncInfoSettingTips"}),children:(0,e.jsx)(l.Z,{})}),(0,e.jsx)(s.Z.Item,{label:a.formatMessage({id:"pages.setting.enableAutoExtendToken"}),name:"enableAutoExtendToken",help:a.formatMessage({id:"pages.setting.enableAutoExtendTokenTips"}),children:(0,e.jsx)(l.Z,{})}),(0,e.jsx)(s.Z.Item,{label:a.formatMessage({id:"pages.setting.enableUserCustomUploadBase64"}),name:"enableUserCustomUploadBase64",help:a.formatMessage({id:"pages.setting.enableUserCustomUploadBase64Tips"}),children:(0,e.jsx)(l.Z,{})}),(0,e.jsx)(s.Z.Item,{label:a.formatMessage({id:"pages.setting.enableSaveUserUploadBase64"}),name:"enableSaveUserUploadBase64",help:a.formatMessage({id:"pages.setting.enableSaveUserUploadBase64Tips"}),children:(0,e.jsx)(l.Z,{})}),(0,e.jsx)(s.Z.Item,{label:a.formatMessage({id:"pages.setting.enableSaveUserUploadLink"}),name:"enableSaveUserUploadLink",help:a.formatMessage({id:"pages.setting.enableSaveUserUploadLinkTips"}),children:(0,e.jsx)(l.Z,{})}),(0,e.jsx)(s.Z.Item,{label:a.formatMessage({id:"pages.setting.enableSaveGeneratedImage"}),name:"enableSaveGeneratedImage",help:a.formatMessage({id:"pages.setting.enableSaveGeneratedImageTips"}),children:(0,e.jsx)(l.Z,{})}),(0,e.jsx)(s.Z.Item,{label:a.formatMessage({id:"pages.setting.enableSaveIntermediateImage"}),name:"enableSaveIntermediateImage",help:a.formatMessage({id:"pages.setting.enableSaveIntermediateImageTips"}),children:(0,e.jsx)(l.Z,{})}),(0,e.jsx)(s.Z.Item,{label:a.formatMessage({id:"pages.setting.enableConvertOfficialLink"}),name:"enableConvertOfficialLink",help:a.formatMessage({id:"pages.setting.enableConvertOfficialLinkTips"}),children:(0,e.jsx)(l.Z,{})}),(0,e.jsx)(s.Z.Item,{label:a.formatMessage({id:"pages.setting.enableMjTranslate"}),name:"enableMjTranslate",help:a.formatMessage({id:"pages.setting.enableMjTranslateTips"}),children:(0,e.jsx)(l.Z,{})}),(0,e.jsx)(s.Z.Item,{label:a.formatMessage({id:"pages.setting.enableNijiTranslate"}),name:"enableNijiTranslate",help:a.formatMessage({id:"pages.setting.enableNijiTranslateTips"}),children:(0,e.jsx)(l.Z,{})}),(0,e.jsx)(s.Z.Item,{label:a.formatMessage({id:"pages.setting.enableConvertNijiToMj"}),name:"enableConvertNijiToMj",help:a.formatMessage({id:"pages.setting.enableConvertNijiToMjTips"}),children:(0,e.jsx)(l.Z,{})}),(0,e.jsx)(s.Z.Item,{label:a.formatMessage({id:"pages.setting.enableConvertNijiToNijiBot"}),name:"enableConvertNijiToNijiBot",help:a.formatMessage({id:"pages.setting.enableConvertNijiToNijiBotTips"}),children:(0,e.jsx)(l.Z,{})}),(0,e.jsx)(s.Z.Item,{label:a.formatMessage({id:"pages.setting.enableAutoLogin"}),name:"enableAutoLogin",help:a.formatMessage({id:"pages.setting.enableAutoLoginTips"}),children:(0,e.jsx)(l.Z,{})})]})})})]})})})},De=Fe}}]);
