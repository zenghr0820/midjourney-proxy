"use strict";(self.webpackChunkmidjourney_proxy_admin=self.webpackChunkmidjourney_proxy_admin||[]).push([[971],{70069:function(ye,J,a){a.r(J),a.d(J,{default:function(){return ce}});var Y=a(15009),L=a.n(Y),q=a(99289),Q=a.n(q),_=a(5574),h=a.n(_),x=a(67294),ee=a(74981),Ie=a(90252),Ce=a(22777),e=a(85893),te=function(l){var D=l.value,f=D===void 0?{}:D,t=l.onChange,R=x.useState(JSON.stringify(f,null,2)),y=h()(R,2),Z=y[0],c=y[1],O=x.useState(!0),A=h()(O,2),E=A[0],I=A[1];(0,x.useEffect)(function(){var k=JSON.stringify(f),M=JSON.stringify(JSON.parse(Z));k!==M&&c(JSON.stringify(f,null,2))},[f]);var C=function(M){c(M);try{var N=JSON.parse(M);I(!0),t&&t(N)}catch(V){I(!1)}};return(0,e.jsxs)("div",{style:{width:"100%"},children:[(0,e.jsx)(ee.ZP,{mode:"json",theme:"textmate",value:Z,onChange:C,name:"json-editor",editorProps:{$blockScrolling:!0},height:"auto",maxLines:1/0,setOptions:{showLineNumbers:!0,tabSize:2,useWorker:!1},style:{width:"100%",minHeight:"80px"},fontSize:14,lineHeight:19,showPrintMargin:!0,showGutter:!0,highlightActiveLine:!0}),!E&&(0,e.jsx)("div",{style:{color:"red",marginTop:"8px"},children:"JSON \u683C\u5F0F\u9519\u8BEF\uFF0C\u8BF7\u68C0\u67E5\u8F93\u5165\uFF01"})]})},d=te,S=a(66927),ne=function(){var l=document.createElement("div");l.id="full-screen-loading",l.style.position="fixed",l.style.top="0",l.style.left="0",l.style.width="100%",l.style.height="100%",l.style.zIndex="100",l.style.backgroundColor="#fff",l.innerHTML=`
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
  `,document.body.appendChild(l)},ae=function(){var l=document.getElementById("full-screen-loading");l&&l.parentNode&&l.parentNode.removeChild(l)},se=a(87740),ie=a(60219),re=a(90930),le=a(94272),n=a(53025),p=a(2453),oe=a(74330),$=a(42075),ge=a(40056),de=a(83062),u=a(55102),F=a(83622),me=a(28248),H=a(71230),B=a(15746),U=a(4393),s=a(72269),i=a(74656),j=a(37804),pe=function(){var l=n.Z.useForm(),D=h()(l,1),f=D[0],t=(0,le.useIntl)(),R=(0,x.useState)(!1),y=h()(R,2),Z=y[0],c=y[1],O=function(){c(!0),(0,S.iE)().then(function(r){c(!1),r.success&&f.setFieldsValue(r.data)})};(0,x.useEffect)(function(){O()},[]);var A=function(){f.validateFields().then(function(r){c(!0),(0,S.rF)(r).then(function(b){c(!1),b.success?(p.ZP.success(t.formatMessage({id:"pages.setting.saveSuccess"})),O()):p.ZP.error(b.message||t.formatMessage({id:"pages.setting.error"}))})}).catch(function(){p.ZP.error(t.formatMessage({id:"pages.setting.error"}))})},E=(0,x.useState)(""),I=h()(E,2),C=I[0],k=I[1],M=(0,x.useState)(""),N=h()(M,2),V=N[0],ue=N[1],fe=(0,x.useState)(!1),W=h()(fe,2),he=W[0],G=W[1],xe=n.Z.useForm(),je=h()(xe,1),P=je[0],be=(0,x.useState)(""),K=h()(be,2),X=K[0],ve=K[1],Ze=function(){var m=Q()(L()().mark(function r(b){var T,v;return L()().wrap(function(g){for(;;)switch(g.prev=g.next){case 0:return G(!1),ne(),T=new Promise(function(o){return setTimeout(o,3e3)}),v=null,g.prev=4,g.next=7,(0,S.df)({password:b});case 7:v=g.sent,v.success?p.ZP.success(v.message):v.code===504&&p.ZP.warning("\u7CFB\u7EDF\u6B63\u5728\u52A0\u8F7D\u4E2D..."),g.next=14;break;case 11:g.prev=11,g.t0=g.catch(4),p.ZP.warning("\u7CFB\u7EDF\u6B63\u5728\u52A0\u8F7D\u4E2D...");case 14:return g.prev=14,g.next=17,T;case 17:return ae(),g.finish(14);case 19:case"end":return g.stop()}},r,null,[[4,11,14,19]])}));return function(b){return m.apply(this,arguments)}}(),Me=function(){var m=Q()(L()().mark(function r(b,T){var v,w;return L()().wrap(function(o){for(;;)switch(o.prev=o.next){case 0:return o.prev=0,c(!0),v={Host:b,ApiSecret:T},o.next=5,(0,S.gU)(v);case 5:w=o.sent,w.success?p.ZP.success(t.formatMessage({id:"pages.setting.migrateSuccess"})):p.ZP.error(w.message),o.next=12;break;case 9:o.prev=9,o.t0=o.catch(0),p.ZP.error(o.t0);case 12:return o.prev=12,c(!1),o.finish(12);case 15:case"end":return o.stop()}},r,null,[[0,9,12,15]])}));return function(b,T){return m.apply(this,arguments)}}(),Se=function(){C?Me(C,V):p.ZP.warning(t.formatMessage({id:"pages.setting.migrateTips"}))};return(0,e.jsx)(re._z,{children:(0,e.jsx)(n.Z,{form:f,labelAlign:"left",layout:"horizontal",labelCol:{span:6},wrapperCol:{span:18},children:(0,e.jsxs)(oe.Z,{spinning:Z,children:[(0,e.jsxs)($.Z,{style:{marginBottom:"10px",display:"flex",justifyContent:"space-between"},children:[(0,e.jsx)(ge.Z,{type:"info",style:{paddingTop:"4px",paddingBottom:"4px"},description:t.formatMessage({id:"pages.setting.tips"})}),(0,e.jsxs)($.Z,{children:[(0,e.jsx)(de.Z,{placement:"bottom",title:(0,e.jsxs)("div",{style:{display:"flex",flexDirection:"column",gap:8,padding:8},children:[(0,e.jsx)(u.Z,{style:{marginBottom:8},placeholder:"mjplus host",value:C,onChange:function(r){return k(r.target.value)}}),(0,e.jsx)(u.Z,{placeholder:"mj-api-secret",value:V,onChange:function(r){return ue(r.target.value)}})]}),children:(0,e.jsx)(F.ZP,{loading:Z,type:"primary",ghost:!0,onClick:Se,children:t.formatMessage({id:"pages.setting.migrate"})})}),(0,e.jsx)(F.ZP,{loading:Z,icon:(0,e.jsx)(se.Z,{}),type:"primary",onClick:function(){return G(!0)},children:t.formatMessage({id:"pages.setting.restart"})}),(0,e.jsx)(F.ZP,{loading:Z,icon:(0,e.jsx)(ie.Z,{}),type:"primary",onClick:A,children:t.formatMessage({id:"pages.setting.save"})}),(0,e.jsx)(me.Z,{open:he,onCancel:function(){G(!1),P.resetFields()},onOk:function(){P.validateFields().then(function(){Ze(X),P.resetFields()}).catch(function(r){console.log("Validation failed:",r)})},okText:"\u786E\u8BA4",cancelText:"\u53D6\u6D88",children:(0,e.jsx)(n.Z,{form:P,layout:"vertical",initialValues:{remember:!0},children:(0,e.jsx)(n.Z.Item,{label:t.formatMessage({id:"pages.setting.restartModalTip"}),name:"password",rules:[{required:!0,message:t.formatMessage({id:"pages.setting.restartModalTip"})}],children:(0,e.jsx)(u.Z.Password,{placeholder:t.formatMessage({id:"pages.setting.restartModalTip"}),value:X,onChange:function(r){ve(r.target.value),r.target.value.trim()!==""&&f.validateFields(["password"])}})})})})]})]}),(0,e.jsxs)(H.Z,{gutter:16,children:[(0,e.jsx)(B.Z,{span:12,children:(0,e.jsxs)(U.Z,{title:t.formatMessage({id:"pages.setting.accountSetting"}),children:[(0,e.jsx)(n.Z.Item,{label:t.formatMessage({id:"pages.setting.enableSwagger"}),name:"enableSwagger",extra:f.getFieldValue("enableSwagger")?(0,e.jsx)("a",{href:"/swagger",target:"_blank",rel:"noreferrer",children:t.formatMessage({id:"pages.setting.swaggerLink"})}):"",children:(0,e.jsx)(s.Z,{})}),(0,e.jsx)(n.Z.Item,{label:t.formatMessage({id:"pages.setting.databaseType"}),name:"databaseType",children:(0,e.jsxs)(i.Z,{allowClear:!0,children:[(0,e.jsx)(i.Z.Option,{value:"LiteDB",children:"LiteDB"}),(0,e.jsx)(i.Z.Option,{value:"MongoDB",children:"MongoDB"}),(0,e.jsx)(i.Z.Option,{value:"SQLite",children:"SQLite"}),(0,e.jsx)(i.Z.Option,{value:"MySQL",children:"MySQL"}),(0,e.jsx)(i.Z.Option,{value:"PostgreSQL",children:"PostgreSQL"}),(0,e.jsx)(i.Z.Option,{value:"SQLServer",children:"SQLServer"})]})}),(0,e.jsx)(n.Z.Item,{label:t.formatMessage({id:"pages.setting.databaseConnectionString"}),name:"databaseConnectionString",extra:(0,e.jsx)(e.Fragment,{children:(0,e.jsx)(F.ZP,{style:{marginTop:8},type:"primary",onClick:function(){c(!0),(0,S.yk)().then(function(r){c(!1),r.success?p.ZP.success(t.formatMessage({id:"pages.setting.connectSuccess"})):p.ZP.error(r.message||t.formatMessage({id:"pages.setting.connectError"}))})},children:t.formatMessage({id:"pages.setting.testConnect"})})}),children:(0,e.jsx)(u.Z,{})}),(0,e.jsx)(n.Z.Item,{label:t.formatMessage({id:"pages.setting.databaseName"}),name:"databaseName",children:(0,e.jsx)(u.Z,{})}),(0,e.jsx)(n.Z.Item,{label:t.formatMessage({id:"pages.setting.isAutoMigrate"}),name:"isAutoMigrate",tooltip:t.formatMessage({id:"pages.setting.isAutoMigrateTips"}),children:(0,e.jsx)(s.Z,{})}),(0,e.jsx)(n.Z.Item,{label:t.formatMessage({id:"pages.setting.maxCount"}),name:"maxCount",children:(0,e.jsx)(j.Z,{min:-1})}),(0,e.jsx)(n.Z.Item,{label:t.formatMessage({id:"pages.setting.accountChooseRule"}),name:"accountChooseRule",children:(0,e.jsxs)(i.Z,{allowClear:!0,children:[(0,e.jsx)(i.Z.Option,{value:"BestWaitIdle",children:"BestWaitIdle"}),(0,e.jsx)(i.Z.Option,{value:"Random",children:"Random"}),(0,e.jsx)(i.Z.Option,{value:"Weight",children:"Weight"}),(0,e.jsx)(i.Z.Option,{value:"Polling",children:"Polling"})]})}),(0,e.jsx)(n.Z.Item,{label:t.formatMessage({id:"pages.setting.discordConfig"}),name:"ngDiscord",children:(0,e.jsx)(d,{})}),(0,e.jsx)(n.Z.Item,{label:t.formatMessage({id:"pages.setting.proxyConfig"}),name:"proxy",children:(0,e.jsx)(d,{})}),(0,e.jsx)(n.Z.Item,{label:t.formatMessage({id:"pages.setting.imageStorageType"}),name:"imageStorageType",children:(0,e.jsxs)(i.Z,{allowClear:!0,children:[(0,e.jsx)(i.Z.Option,{value:"NONE",children:"NULL"}),(0,e.jsx)(i.Z.Option,{value:"LOCAL",children:"LOCAL"}),(0,e.jsx)(i.Z.Option,{value:"OSS",children:"Aliyun OSS"}),(0,e.jsx)(i.Z.Option,{value:"COS",children:"Tencent COS"}),(0,e.jsx)(i.Z.Option,{value:"R2",children:"Cloudflare R2"})]})}),(0,e.jsx)(n.Z.Item,{label:t.formatMessage({id:"pages.setting.localStorage"}),name:"localStorage",children:(0,e.jsx)(d,{})}),(0,e.jsx)(n.Z.Item,{label:t.formatMessage({id:"pages.setting.aliyunOss"}),name:"aliyunOss",children:(0,e.jsx)(d,{})}),(0,e.jsx)(n.Z.Item,{label:t.formatMessage({id:"pages.setting.tencentCos"}),name:"tencentCos",children:(0,e.jsx)(d,{})}),(0,e.jsx)(n.Z.Item,{label:t.formatMessage({id:"pages.setting.cloudflareR2"}),name:"cloudflareR2",children:(0,e.jsx)(d,{})}),(0,e.jsx)(n.Z.Item,{label:t.formatMessage({id:"pages.setting.replicate"}),name:"replicate",children:(0,e.jsx)(d,{})}),(0,e.jsx)(n.Z.Item,{label:t.formatMessage({id:"pages.setting.translate"}),name:"translateWay",children:(0,e.jsxs)(i.Z,{allowClear:!0,children:[(0,e.jsx)(i.Z.Option,{value:"NULL",children:"NULL"}),(0,e.jsx)(i.Z.Option,{value:"BAIDU",children:"BAIDU"}),(0,e.jsx)(i.Z.Option,{value:"GPT",children:"GPT"})]})}),(0,e.jsx)(n.Z.Item,{label:t.formatMessage({id:"pages.setting.baiduTranslate"}),name:"baiduTranslate",children:(0,e.jsx)(d,{})}),(0,e.jsx)(n.Z.Item,{label:t.formatMessage({id:"pages.setting.openai"}),name:"openai",children:(0,e.jsx)(d,{})}),(0,e.jsx)(n.Z.Item,{label:t.formatMessage({id:"pages.setting.smtp"}),name:"smtp",children:(0,e.jsx)(d,{})}),(0,e.jsx)(n.Z.Item,{label:t.formatMessage({id:"pages.setting.notifyHook"}),name:"notifyHook",children:(0,e.jsx)(u.Z,{})}),(0,e.jsx)(n.Z.Item,{label:t.formatMessage({id:"pages.setting.notifyPoolSize"}),name:"notifyPoolSize",children:(0,e.jsx)(j.Z,{})}),(0,e.jsx)(n.Z.Item,{label:t.formatMessage({id:"pages.setting.captchaServer"}),name:"captchaServer",help:t.formatMessage({id:"pages.setting.captchaServerTip"}),children:(0,e.jsx)(u.Z,{})}),(0,e.jsx)(n.Z.Item,{label:t.formatMessage({id:"pages.setting.captchaNotifyHook"}),name:"captchaNotifyHook",help:t.formatMessage({id:"pages.setting.captchaNotifyHookTip"}),children:(0,e.jsx)(u.Z,{})}),(0,e.jsx)(n.Z.Item,{label:t.formatMessage({id:"pages.setting.captchaNotifySecret"}),name:"captchaNotifySecret",help:t.formatMessage({id:"pages.setting.captchaNotifySecretTip"}),children:(0,e.jsx)(u.Z,{})})]})}),(0,e.jsx)(B.Z,{span:12,children:(0,e.jsxs)(U.Z,{title:t.formatMessage({id:"pages.setting.otherSetting"}),children:[(0,e.jsx)(n.Z.Item,{label:t.formatMessage({id:"pages.setting.enableAccountSponsor"}),name:"enableAccountSponsor",help:t.formatMessage({id:"pages.setting.enableAccountSponsorTips"}),children:(0,e.jsx)(s.Z,{})}),(0,e.jsx)(n.Z.Item,{label:t.formatMessage({id:"pages.setting.isVerticalDomain"}),name:"isVerticalDomain",help:t.formatMessage({id:"pages.setting.isVerticalDomainTips"}),children:(0,e.jsx)(s.Z,{})}),(0,e.jsx)(n.Z.Item,{label:t.formatMessage({id:"pages.setting.enableRegister"}),name:"enableRegister",children:(0,e.jsx)(s.Z,{})}),(0,e.jsx)(n.Z.Item,{label:t.formatMessage({id:"pages.setting.registerUserDefaultDayLimit"}),name:"registerUserDefaultDayLimit",children:(0,e.jsx)(j.Z,{min:-1})}),(0,e.jsx)(n.Z.Item,{label:t.formatMessage({id:"pages.setting.registerUserDefaultTotalLimit"}),name:"registerUserDefaultTotalLimit",children:(0,e.jsx)(j.Z,{min:-1})}),(0,e.jsx)(n.Z.Item,{label:t.formatMessage({id:"pages.setting.registerUserDefaultCoreSize"}),name:"registerUserDefaultCoreSize",children:(0,e.jsx)(j.Z,{min:-1})}),(0,e.jsx)(n.Z.Item,{label:t.formatMessage({id:"pages.setting.registerUserDefaultQueueSize"}),name:"registerUserDefaultQueueSize",children:(0,e.jsx)(j.Z,{min:-1})}),(0,e.jsx)(n.Z.Item,{label:t.formatMessage({id:"pages.setting.enableGuest"}),name:"enableGuest",children:(0,e.jsx)(s.Z,{})}),(0,e.jsx)(n.Z.Item,{label:t.formatMessage({id:"pages.setting.guestDefaultDayLimit"}),name:"guestDefaultDayLimit",children:(0,e.jsx)(j.Z,{min:-1})}),(0,e.jsx)(n.Z.Item,{label:t.formatMessage({id:"pages.setting.guestDefaultCoreSize"}),name:"guestDefaultCoreSize",children:(0,e.jsx)(j.Z,{min:-1})}),(0,e.jsx)(n.Z.Item,{label:t.formatMessage({id:"pages.setting.guestDefaultQueueSize"}),name:"guestDefaultQueueSize",children:(0,e.jsx)(j.Z,{min:-1})}),(0,e.jsx)(n.Z.Item,{label:t.formatMessage({id:"pages.setting.bannedLimiting"}),name:"bannedLimiting",children:(0,e.jsx)(d,{})}),(0,e.jsx)(n.Z.Item,{label:t.formatMessage({id:"pages.setting.ipRateLimiting"}),name:"ipRateLimiting",children:(0,e.jsx)(d,{})}),(0,e.jsx)(n.Z.Item,{label:t.formatMessage({id:"pages.setting.ipBlackRateLimiting"}),name:"ipBlackRateLimiting",children:(0,e.jsx)(d,{})}),(0,e.jsx)(n.Z.Item,{label:t.formatMessage({id:"pages.setting.notify"}),name:"notify",children:(0,e.jsx)(u.Z.TextArea,{autoSize:{minRows:1,maxRows:10}})})]})})]}),(0,e.jsx)(H.Z,{gutter:16,style:{marginTop:"16px"},children:(0,e.jsx)(B.Z,{span:12,children:(0,e.jsxs)(U.Z,{title:t.formatMessage({id:"pages.setting.discordSetting"}),children:[(0,e.jsx)(n.Z.Item,{label:t.formatMessage({id:"pages.setting.enableAutoGetPrivateId"}),name:"enableAutoGetPrivateId",help:t.formatMessage({id:"pages.setting.enableAutoGetPrivateIdTips"}),children:(0,e.jsx)(s.Z,{})}),(0,e.jsx)(n.Z.Item,{label:t.formatMessage({id:"pages.setting.enableAutoVerifyAccount"}),name:"enableAutoVerifyAccount",help:t.formatMessage({id:"pages.setting.enableAutoVerifyAccountTips"}),children:(0,e.jsx)(s.Z,{})}),(0,e.jsx)(n.Z.Item,{label:t.formatMessage({id:"pages.setting.enableAutoSyncInfoSetting"}),name:"enableAutoSyncInfoSetting",help:t.formatMessage({id:"pages.setting.enableAutoSyncInfoSettingTips"}),children:(0,e.jsx)(s.Z,{})}),(0,e.jsx)(n.Z.Item,{label:t.formatMessage({id:"pages.setting.enableAutoExtendToken"}),name:"enableAutoExtendToken",help:t.formatMessage({id:"pages.setting.enableAutoExtendTokenTips"}),children:(0,e.jsx)(s.Z,{})}),(0,e.jsx)(n.Z.Item,{label:t.formatMessage({id:"pages.setting.enableUserCustomUploadBase64"}),name:"enableUserCustomUploadBase64",help:t.formatMessage({id:"pages.setting.enableUserCustomUploadBase64Tips"}),children:(0,e.jsx)(s.Z,{})}),(0,e.jsx)(n.Z.Item,{label:t.formatMessage({id:"pages.setting.enableSaveUserUploadBase64"}),name:"enableSaveUserUploadBase64",help:t.formatMessage({id:"pages.setting.enableSaveUserUploadBase64Tips"}),children:(0,e.jsx)(s.Z,{})}),(0,e.jsx)(n.Z.Item,{label:t.formatMessage({id:"pages.setting.enableSaveUserUploadLink"}),name:"enableSaveUserUploadLink",help:t.formatMessage({id:"pages.setting.enableSaveUserUploadLinkTips"}),children:(0,e.jsx)(s.Z,{})}),(0,e.jsx)(n.Z.Item,{label:t.formatMessage({id:"pages.setting.enableSaveGeneratedImage"}),name:"enableSaveGeneratedImage",help:t.formatMessage({id:"pages.setting.enableSaveGeneratedImageTips"}),children:(0,e.jsx)(s.Z,{})}),(0,e.jsx)(n.Z.Item,{label:t.formatMessage({id:"pages.setting.enableSaveIntermediateImage"}),name:"enableSaveIntermediateImage",help:t.formatMessage({id:"pages.setting.enableSaveIntermediateImageTips"}),children:(0,e.jsx)(s.Z,{})}),(0,e.jsx)(n.Z.Item,{label:t.formatMessage({id:"pages.setting.enableAutoDeleteImagineMessage"}),name:"enableAutoDeleteImagineMessage",help:t.formatMessage({id:"pages.setting.enableAutoDeleteImagineMessageTips"}),children:(0,e.jsx)(s.Z,{})}),(0,e.jsx)(n.Z.Item,{label:t.formatMessage({id:"pages.setting.enableConvertOfficialLink"}),name:"enableConvertOfficialLink",help:t.formatMessage({id:"pages.setting.enableConvertOfficialLinkTips"}),children:(0,e.jsx)(s.Z,{})}),(0,e.jsx)(n.Z.Item,{label:t.formatMessage({id:"pages.setting.enableMjTranslate"}),name:"enableMjTranslate",help:t.formatMessage({id:"pages.setting.enableMjTranslateTips"}),children:(0,e.jsx)(s.Z,{})}),(0,e.jsx)(n.Z.Item,{label:t.formatMessage({id:"pages.setting.enableNijiTranslate"}),name:"enableNijiTranslate",help:t.formatMessage({id:"pages.setting.enableNijiTranslateTips"}),children:(0,e.jsx)(s.Z,{})}),(0,e.jsx)(n.Z.Item,{label:t.formatMessage({id:"pages.setting.enableConvertNijiToMj"}),name:"enableConvertNijiToMj",help:t.formatMessage({id:"pages.setting.enableConvertNijiToMjTips"}),children:(0,e.jsx)(s.Z,{})}),(0,e.jsx)(n.Z.Item,{label:t.formatMessage({id:"pages.setting.enableConvertNijiToNijiBot"}),name:"enableConvertNijiToNijiBot",help:t.formatMessage({id:"pages.setting.enableConvertNijiToNijiBotTips"}),children:(0,e.jsx)(s.Z,{})}),(0,e.jsx)(n.Z.Item,{label:t.formatMessage({id:"pages.setting.enableAutoLogin"}),name:"enableAutoLogin",help:t.formatMessage({id:"pages.setting.enableAutoLoginTips"}),children:(0,e.jsx)(s.Z,{})})]})})})]})})})},ce=pe}}]);
